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
    public GeneratedRoute SearchForRoute([FromQuery] string departure, [FromQuery] string destination)
    {
        var departureAerodrome = _aerodromeService.GetAerodrome(departure);
        var destinationAerodrome = _aerodromeService.GetAerodrome(destination);

        if (departureAerodrome == null || destinationAerodrome == null)
            return new GeneratedRoute()
            {
                Success = false,
                Message = "Departure or destination aerodrome code invalid."
            };

        var startPoints = (departureAerodrome.Sids.Any() ? 
            departureAerodrome.Sids.Select(t => t.Point)
                .Concat(departureAerodrome.Sids.SelectMany(s => s.Transitions.Select(t => t.Point)))
                .Where(t => t != null).Cast<ICodePoint>().Distinct() 
            :
            _codepointService.GetCodepointNear(departureAerodrome.Location)).ToList();

        var endPoints = destinationAerodrome.Stars.Any() ? 
            destinationAerodrome.Stars.Select(t => t.Point).Concat(
            destinationAerodrome.Stars.SelectMany(s => s.Transitions.Select(t => t.Point)))
            .Distinct().ToList() : null;
        
        if (!startPoints.Any())
            return new GeneratedRoute
            {
                Success = false,
                Message = "Unable to locate entry points near aerodrome."
            };

        var openList = startPoints.Where(p => p.AirwayPoints.Any()).Select(p => new AirwaySearch(
            null,
            p,
            p.Code,
            "",
            0,
            0,
            p.Location.GetDistance(destinationAerodrome.Location)
        )).ToList();
        var closedList = new List<AirwaySearch>();

        AirwaySearch? current = null;
        
        while (openList.Count > 0)
        {
            current = openList.MinBy(l => l.EstimatedTotalDistance)!;

            closedList.Add(current);
            openList.Remove(current);

            if ((endPoints != null && endPoints.Any(p => p == current.Point)) ||
                current.Point.Location.GetDistance(destinationAerodrome.Location) < 5)
                break;
            
            var traversalPoints = current.Point.AirwayPoints
                .Where(p => p.NextLeg != null)
                .Select(p => p.NextLeg!)
                .Select(p => new AirwayLegSearch
                {
                    AirwayLeg = p,
                    Airway = p.Airway,
                    AirwayPoint = p.EndPoint,
                    High = p.Level == AirwayLegLevel.HIGH
                }).ToList();
            traversalPoints.AddRange(current.Point.AirwayPoints.Where(p => p.Airway.TwoWay)
                .Where(p => p.LastLeg != null)
                .Select(p => p.LastLeg!)
                .Select(p => new AirwayLegSearch
                {
                    AirwayLeg = p,
                    Airway = p.Airway,
                    AirwayPoint = p.StartPoint,
                    High = p.Level == AirwayLegLevel.HIGH
                }));
            
            
            foreach (var traversalPoint in traversalPoints)
            {
                var code = traversalPoint.AirwayPoint.Point.Code + " " + traversalPoint.Airway.Code;
                
                if (closedList.Any(l => l.PointAirwayCode == code))
                    continue;
 
                if (openList.All(l => l.PointAirwayCode != code))
                {
                    var airways = current.Airways + (current.LastAirway != traversalPoint.Airway.Code ? 1 : 0);
                    openList.Insert(0, 
                        new AirwaySearch (
                            current,
                            traversalPoint.AirwayPoint.Point,
                            code,
                            traversalPoint.Airway.Code,
                            airways,
                            current.CurrentDistance + traversalPoint.AirwayLeg.Distance,
                            current.CurrentDistance +
                            airways * AverageLegDistance +
                            traversalPoint.AirwayLeg.Distance * (traversalPoint.AirwayLeg.Level == AirwayLegLevel.LOW ? 1.5 : 1) + 
                            traversalPoint.AirwayPoint.Point.Location.GetDistance(destinationAerodrome.Location)
                        ));
                }
                else
                {
                    var search = openList.First(l => l.PointAirwayCode == code);
                    if (search.CurrentDistance > current.CurrentDistance + traversalPoint.AirwayLeg.Distance)
                        search.Parent = current;
                }
            }
        }

        if (current != null && openList.Count > 0)
        {
            var segments = new List<string>();
            var lastAirway = "";
            while (current != null)
            {
                if (current.LastAirway != lastAirway)
                {
                    segments.Add(current.Point.Code);
                    if(!string.IsNullOrWhiteSpace(current.LastAirway))
                        segments.Add(current.LastAirway);
                    lastAirway = current.LastAirway;
                }

                current = current.Parent;
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