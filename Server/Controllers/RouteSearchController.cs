using ATCTools.Server.Models;
using ATCTools.Server.Services;
using Microsoft.AspNetCore.Mvc;
using ATCTools.Shared.Models;

namespace ATCTools.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class RouteSearchController
{
    private const int AverageLegDistance = 100;
    
    private readonly ILogger<RouteSearchController> _logger;
    private readonly AerodromeService _aerodromeService;
    private readonly CodepointService _codepointService;
    
    public RouteSearchController(
        ILogger<RouteSearchController> logger,
        AerodromeService aerodromeService,
        CodepointService codepointService)
    {
        _logger = logger;
        _aerodromeService = aerodromeService;
        _codepointService = codepointService;
    }

    [HttpGet]
    public GeneratedRoute SearchForRoute([FromQuery] string departure, [FromQuery] string destination, [FromQuery] AircraftType? Type)
    {
        var departureAerodrome = _aerodromeService.GetAerodrome(departure);
        var destinationAerodrome = _aerodromeService.GetAerodrome(destination);

        var allTypes = Type is null or AircraftType.BOTH;

        if (departureAerodrome == null || destinationAerodrome == null)
            return new GeneratedRoute()
            {
                Success = false,
                Message = "Departure or destination aerodrome code invalid."
            };

        var sids = departureAerodrome.Sids
            .Where(s => allTypes || s.AircraftType == AircraftType.BOTH || s.AircraftType == Type).ToList();
        
        var startPoints = (sids.Any()
            ? sids.Select(t => t.Point).Concat(
                    sids.SelectMany(s => s.Transitions.Select(t => t.Point)))
                .Where(t => t != null).Cast<ICodePoint>().Distinct()
            : _codepointService.GetCodepointsNear(departureAerodrome.Location, 2)).ToList();
         //var startPoints = _codepointService.GetCodepointsNear(departureAerodrome.Location, 2).ToList();
         
         var radar = sids?.Where(s => s.Transitions
             .Any(t => t.Type == TransitionType.RADAR)).Select(t => t.Point!).ToList() ?? new List<ICodePoint>();

         var stars = destinationAerodrome.Stars
             .Where(s => allTypes || s.AircraftType == AircraftType.BOTH || s.AircraftType == Type).ToList();
         
        var endPoints = stars.Any() ? 
            stars.Select(t => t.Point)
            .Concat(stars.SelectMany(s => 
                s.Transitions.Where(t => allTypes || t.AircraftType == AircraftType.BOTH || t.AircraftType == Type).Select(t => t.Point)))
            .Distinct().ToList() : null;
        
        if (!startPoints.Any())
            return new GeneratedRoute
            {
                Success = false,
                Message = "Unable to locate entry points near aerodrome."
            };

        var openList = startPoints.Select(p => new AirwaySearch(
            null,
            p,
            p.Code,
            "",
            0,
            0,
            p.Location.GetDistance(destinationAerodrome.Location)
        )).ToList();
        var closedList = new List<AirwaySearch>();

        AirwaySearch? final = null;
        
        while (openList.Count > 0)
        {
            var current = openList.MinBy(l => l.EstimatedTotalDistance)!;

            closedList.Add(current);
            openList.Remove(current);

            if ((endPoints != null && endPoints.Any(p => p == current.Point)) ||
                (endPoints == null && current.Point.Location.GetDistance(destinationAerodrome.Location) < 5))
            //if(current.Point.Location.GetDistance(destinationAerodrome.Location) < 2)
            {
                final = current;
                break;
            }
            
            var traversalPoints = current.Point.AirwayPoints
                .Where(p => p.NextLeg != null)
                .Select(p => p.NextLeg!)
                .Select(p => new RouteLegSearch
                {
                    AirwayLeg = p,
                    Airway = p.Airway,
                    AirwayPoint = p.EndPoint,
                    Point = p.EndPoint.Point
                }).ToList();
            traversalPoints.AddRange(current.Point.AirwayPoints.Where(p => p.Airway.TwoWay)
                .Where(p => p.LastLeg != null)
                .Select(p => p.LastLeg!)
                .Select(p => new RouteLegSearch
                {
                    AirwayLeg = p,
                    Airway = p.Airway,
                    AirwayPoint = p.EndPoint,
                    Point = p.EndPoint.Point
                }));

            traversalPoints.AddRange(_codepointService.GetCodepointsNear(current.Point.Location, 30)
                .Where(p => p.Code != current.Point.Code)                
                .Select(p =>
                new RouteLegSearch
                {
                    Point = p
                }));
            
            foreach (var traversalPoint in traversalPoints)
            {
                var code = traversalPoint.Point.Code + " " + (traversalPoint.Airway?.Code ?? "DCT");
                
                if (closedList.Any(l => l.PointAirwayCode == code))
                    continue;

                var switchingAirway = current.LastAirway != (traversalPoint.Airway?.Code ?? "DCT");
                double weightedLegDistance;
                if (traversalPoint.AirwayLeg != null)
                    weightedLegDistance = traversalPoint.AirwayLeg.Distance *
                                          (traversalPoint.AirwayLeg.Level == AirwayLegLevel.LOW ? 1.1 : 1) *
                                          (switchingAirway ? 1.1 : 1);
                else
                    weightedLegDistance = traversalPoint.Point.Location.GetDistance(current.Point.Location) * 
                                          (radar.Contains(traversalPoint.Point) ? 1 : 1.5);
 
                if (openList.All(l => l.PointAirwayCode != code))
                    openList.Insert(0, 
                        new AirwaySearch (
                            current,
                            traversalPoint.Point,
                            code,
                            traversalPoint.Airway?.Code ?? "DCT",
                            current.Airways + (switchingAirway ? 1 : 0),
                            current.CurrentDistance + weightedLegDistance,
                            current.CurrentDistance + weightedLegDistance + 
                            traversalPoint.Point.Location.GetDistance(destinationAerodrome.Location)
                        ));
                else
                {
                    var search = openList.First(l => l.PointAirwayCode == code);
                    if (search.CurrentDistance > current.CurrentDistance + weightedLegDistance)
                        search.Parent = current;
                }
            }
        }

        if (final != null)
        {
            var segments = new List<string>();
            var lastAirway = "";
            while (final != null)
            {
                if (final.LastAirway != lastAirway)
                {
                    segments.Add(final.Point.Code);
                    if(!string.IsNullOrWhiteSpace(final.LastAirway))
                        segments.Add(final.LastAirway);
                    lastAirway = final.LastAirway;
                }

                final = final.Parent;
            }
            segments.Reverse();
            
            return new GeneratedRoute()
            {
                Success = true,
                Plan = string.Join(' ', segments)
            };
        }
        
        return new GeneratedRoute()
        {
            Success = false,
            Message = "Unable to find route."
        };
    }
}