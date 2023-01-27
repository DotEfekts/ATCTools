using ATCTools.Server.Models;
using ATCTools.Server.Services;
using Microsoft.AspNetCore.Mvc;
using ATCTools.Shared;
using ATCTools.Shared.Models;

namespace ATCTools.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class FlightPlanValidationController
{
    private readonly ILogger<FlightPlanValidationController> _logger;
    private readonly AirwayService _airwayService;
    private readonly AerodromeService _aerodromeService;
    private readonly CodepointService _codepointService;
    
    public FlightPlanValidationController(
        ILogger<FlightPlanValidationController> logger,
        AirwayService airwayService,
        AerodromeService aerodromeService,
        CodepointService codepointService)
    {
        _logger = logger;
        _airwayService = airwayService;
        _aerodromeService = aerodromeService;
        _codepointService = codepointService;
    }

    [HttpPost]
    public PlanValidationResult CheckSegmentValidity(FlightPlan plan)
    {
        var segments = plan.Route.ToUpper().Split(' ');
        var segmentResults = new List<PlanSegmentValidationResult>();

        plan.DepartingAirport = plan.DepartingAirport.ToUpper();
        plan.DestinationAirport = plan.DestinationAirport.ToUpper();

        var result = new PlanValidationResult();

        var routeMap = new List<Location>();
        var waypoints = new List<PlanWaypoint>();

        var departingAerodrome = _aerodromeService.GetAerodrome(plan.DepartingAirport);
        var destinationAerodrome = _aerodromeService.GetAerodrome(plan.DestinationAirport);

        if (destinationAerodrome != null)
        {
            result.AvailableStars = destinationAerodrome.Stars.Select(s =>
                new PlanStar
                {
                    Name = s.Name,
                    AircraftType = s.AircraftType,
                    Runways = s.Runways,
                    Transitions = string.Join(' ',
                        s.Transitions.Select(t =>
                            t.Code + (t.Runways != null ? " (RWY " + t.Runways +")" : "") + 
                            (t.AircraftType != AircraftType.BOTH ? t.AircraftType == AircraftType.JET ? " (JET)" : " (NON-JET)" : "")))
                }).ToList();
        }
        
        if (departingAerodrome == null)
        {
            result.DepartingAirport = new PlanSegmentValidationResult()
            {
                Segment = plan.DepartingAirport,
                StateDetails = "Could not find aerodrome",
                State = ValidationState.INVALID
            };
        }
        else
        {
            result.AvailableSids = departingAerodrome.Sids.Select(s =>
                new PlanSid
                {
                    Name = s.Name,
                    AircraftType = s.AircraftType,
                    Runways = s.Runways,
                    IsRadar = s.Radar,
                    Transitions = string.Join(' ',
                        s.Transitions.Select(t =>
                            t.Type == TransitionType.NAV ? t.Code : "RADAR" + (t.Track.HasValue ? "-" + t.Track : "")))
                }).ToList();
            
            result.DepartingAirport = new PlanSegmentValidationResult()
            {
                Segment = plan.DepartingAirport
            };
            
            waypoints.Add(new PlanWaypoint()
            {
                Code = plan.DepartingAirport,
                Location = departingAerodrome.Location,
                Type = WaypointType.AIRPORT
            });
            
            routeMap.Add(departingAerodrome.Location);
        }

        var invalid = false;
        var international = false;
        var waypoint = true;
        string? lastCodepoint = null;
        AirwayPoint? entryPoint = null;
        AerodromeSid? selectedSid = null;
        AerodromeStar? selectedStar = null;
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if(string.IsNullOrWhiteSpace(segment))
                continue;
            
            if (i == 0 && segment.Contains('/'))
            {
                if (departingAerodrome == null)
                {
                    segmentResults.Add(new PlanSegmentValidationResult()
                    {
                        Segment = segment,
                        State = ValidationState.UNVALIDATED
                    });
                    invalid = true;
                    continue;
                }

                var sidCode = segment.Split('/');
                selectedSid = departingAerodrome.Sids.FirstOrDefault(s => s.Code == sidCode[0]);
                
                if(selectedSid == null)
                {
                    segmentResults.Add(new PlanSegmentValidationResult()
                    {
                        Segment = segment,
                        State = ValidationState.INVALID,
                        StateDetails = "Invalid SID specified for departure aerodrome"
                    });
                    invalid = true;
                    continue;
                }

                var rws = selectedSid.Runways.Split('|');
                if (rws.All(r => sidCode[1] != r))
                {
                    segmentResults.Add(new PlanSegmentValidationResult()
                    {
                        Segment = segment,
                        State = ValidationState.INVALID,
                        StateDetails = "Invalid runway specified for SID"
                    });
                    invalid = true;
                    continue;
                }
                
                segmentResults.Add(new PlanSegmentValidationResult()
                {
                    Segment = segment
                });
                
                continue;
            }

            if (i == segments.Length - 1)
            {
                var finalWaypoint = segment;
                if (!waypoint && segment.Contains('/'))
                {
                    finalWaypoint = segments[i - 1];
                    
                    if (destinationAerodrome == null)
                    {
                        segmentResults.Add(new PlanSegmentValidationResult()
                        {
                            Segment = segment,
                            State = ValidationState.UNVALIDATED
                        });
                        invalid = true;
                        continue;
                    }

                    var starCode = segment.Split('/');
                    selectedStar = destinationAerodrome.Stars.FirstOrDefault(s => s.Code == starCode[0]);
                    
                    if(selectedStar == null)
                    {
                        segmentResults.Add(new PlanSegmentValidationResult()
                        {
                            Segment = segment,
                            State = ValidationState.INVALID,
                            StateDetails = "Invalid STAR specified for departure aerodrome"
                        });
                        invalid = true;
                        continue;
                    }

                    var selectedTransition = selectedStar.Transitions.FirstOrDefault(t => t.Code == finalWaypoint);

                    if (selectedStar.ArrivalCode != segments[i - 1] && selectedTransition == null)
                    {
                        segmentResults.Add(new PlanSegmentValidationResult()
                        {
                            Segment = segment,
                            State = ValidationState.INVALID,
                            StateDetails = "Final waypoint does not match selected STAR"
                        });
                    }
                    
                    var rws = (selectedTransition?.Runways ?? selectedStar.Runways).Split('|');
                    
                    if (rws.All(r => starCode[1] != r))
                    {
                        segmentResults.Add(new PlanSegmentValidationResult()
                        {
                            Segment = segment,
                            State = ValidationState.INVALID,
                            StateDetails = "Invalid runway specified for STAR"
                        });
                        invalid = true;
                    }
                    
                    segmentResults.Add(new PlanSegmentValidationResult()
                    {
                        Segment = segment
                    });
                }

                if (destinationAerodrome != null)
                {
                    var matchingStars = destinationAerodrome.Stars
                        .Where(s => s.ArrivalCode == finalWaypoint || s.Transitions.Any(t => t.Code == finalWaypoint));

                    foreach (var star in matchingStars)
                    {
                        var matchingTransition = star.Transitions.FirstOrDefault(t => t.Code == finalWaypoint);
                        var rws = matchingTransition?.Runways ?? star.Runways;
                        result.AvailableStars.First(s => s.Name == star.Name).RouteWithStar =
                            string.Join(' ', segments[..^(waypoint ? 0 : 1)]) + " " + star.Code + "/" + (rws.Contains('|') ? "<RWY>" : rws);
                    }
                }
                
                if(finalWaypoint != segment)
                    continue;
            }

            if (invalid)
            {
                segmentResults.Add(new PlanSegmentValidationResult()
                {
                    Segment = segment,
                    State = ValidationState.UNVALIDATED
                });
                continue;
            } 
            
            if (international)
            {
                segmentResults.Add(new PlanSegmentValidationResult()
                {
                    Segment = segment,
                    State = ValidationState.INTERNATIONAL
                });
                continue;
            }
            
            if (i == 0 && segment == "DCT")
            {
                segmentResults.Add(new PlanSegmentValidationResult()
                {
                    Segment = segment
                });
                
                continue;
            }

            var segmentResult = new PlanSegmentValidationResult
            {
                Segment = segment
            };

            if (waypoint)
            {
                if (entryPoint == null)
                {
                    var point = _codepointService.GetCodepoint(segment);
                    if (point == null)
                    {
                        segmentResult.State = ValidationState.INVALID;
                        segmentResult.StateDetails = "Could not find waypoint";
                        invalid = true;
                    }
                    else if (i < 2 && departingAerodrome != null)
                    {
                        if (selectedSid != null && !selectedSid.Radar)
                        {
                            var codes = selectedSid.Transitions
                                .Select(t => t.Code).Concat(new[] {selectedSid.DepartureCode})
                                .Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c!);
                            if (codes.All(c => point.Code != c))
                                if (selectedSid.Transitions.Any(t => t.Track != null))
                                {
                                    var track = (int) selectedSid.Transitions.First(t => t.Track != null).Track!;
                                    var departurePoint = _codepointService.GetCodepoint(selectedSid.DepartureCode!);
                                    var bearingToWaypoint = departurePoint!.Location.GetBearingTo(point.Location);
                                    var difference = (bearingToWaypoint - track + 540) % 360 - 180;
                                    if (Math.Abs(difference) > 90)
                                    {
                                        segmentResult.State = ValidationState.WARNING;
                                        segmentResult.StateDetails = "Waypoint after radar transition is behind aircraft";
                                    }

                                    if (segmentResult.State != ValidationState.WARNING &&
                                        departurePoint!.Location.GetDistance(point.Location) > 50)
                                    {
                                        segmentResult.State = ValidationState.WARNING;
                                        segmentResult.StateDetails = "Initial waypoint is greater than 50NM from SID departure point";
                                    }

                                    if (segmentResult.State == ValidationState.WARNING)
                                    {
                                        waypoints.Add(new PlanWaypoint()
                                        {
                                            Code = segment,
                                            Location = point.Location,
                                            Type = WaypointType.WARNING
                                        });
                        
                                        routeMap.Add(point.Location);
                                    }
                                }
                                else
                                {
                                    segmentResult.State = ValidationState.INVALID;
                                    segmentResult.StateDetails = "Initial waypoint does not match selected SID";
                                }
                        }
                        else if (!departingAerodrome.PointHasSid(point) && departingAerodrome.Location.GetDistance(point.Location) > 50)
                        {
                            segmentResult.State = ValidationState.WARNING;
                            segmentResult.StateDetails = "Initial waypoint is greater than 50NM from departure airport";
                        
                            waypoints.Add(new PlanWaypoint()
                            {
                                Code = segment,
                                Location = point.Location,
                                Type = WaypointType.WARNING
                            });
                        
                            routeMap.Add(point.Location);
                        }
                        
                        if(segmentResult.State == ValidationState.VALID)
                        {
                            waypoints.Add(new PlanWaypoint()
                            {
                                Code = segment,
                                Location = point.Location,
                                Type = WaypointType.STANDARD
                            });
                        
                            routeMap.Add(point.Location);
                        }
                    }
                    else
                    {
                        waypoints.Add(new PlanWaypoint()
                        {
                            Code = segment,
                            Location = point.Location,
                            Type = WaypointType.STANDARD
                        });
                        
                        routeMap.Add(point.Location);
                    }

                    if (departingAerodrome != null && point != null)
                    {
                        foreach (var sid in departingAerodrome.Sids)
                        {
                            if (sid.DepartureCode == point.Code ||
                                sid.Transitions.Any(t => t.Code == point.Code))
                                result.AvailableSids.First(s => s.Name == sid.Name).RouteWithSid =
                                    sid.Code + "/" + (sid.Runways.Contains('|') ? "<RWY>" : sid.Runways) + " " + 
                                    point.Code + " " + string.Join(' ', segments[(i + 1)..]);
                        }
                    }
                }
                else
                {
                    var airway = entryPoint.Airway;
                    var exitPoint = entryPoint.Airway.GetPoint(segment);

                    var point = exitPoint;
                    var reverseTravel = false;

                    if (exitPoint == null)
                    {
                        if (airway.AirwayPoints[^1].Type == AirwayPointType.END_INTERNATIONAL ||
                            (airway.TwoWay && airway.AirwayPoints[0].Type == AirwayPointType.START_INTERNATIONAL))
                        {
                            if (airway.AirwayPoints[^1].Type == AirwayPointType.END_INTERNATIONAL)
                                point = airway.AirwayPoints[^1];
                            else
                            {
                                point = airway.AirwayPoints[0];
                                reverseTravel = true;
                            }
                            
                            waypoints.Add(new PlanWaypoint()
                            {
                                Code = point.Point.Code,
                                Location = point.Point.Location,
                                Type = WaypointType.AIRSPACE_BORDER
                            });
                            
                            segmentResult.State = ValidationState.INTERNATIONAL;
                            segmentResult.StateDetails = "Route believed to exit country airspace";
                            international = true;
                        }
                        else
                        {
                            segmentResult.State = ValidationState.INVALID;
                            segmentResult.StateDetails = "Cannot exit airway from this location";
                            waypoints.Last().Type = WaypointType.ERROR;
                            invalid = true;
                        }
                    }
                    else if (!airway.TwoWay && 
                             Array.IndexOf(airway.AirwayPoints, entryPoint) >
                             Array.IndexOf(airway.AirwayPoints, exitPoint))
                    {
                        segmentResult.State = ValidationState.INVALID;
                        segmentResult.StateDetails = "Cannot travel given direction in selected airway";
                        waypoints.Last().Type = WaypointType.ERROR;
                        invalid = true;
                    }
                    else
                    {
                        reverseTravel = Array.IndexOf(airway.AirwayPoints, entryPoint) >
                                        Array.IndexOf(airway.AirwayPoints, exitPoint);
                        
                        segmentResult.State = ValidationState.VALID;
                        
                        waypoints.Add(new PlanWaypoint()
                        {
                            Code = segment,
                            Location = point.Point.Location,
                            Type = WaypointType.STANDARD
                        });
                    }

                    if (departingAerodrome != null && point != null)
                    {
                        var sidPoint = entryPoint;
                        do
                        {
                            foreach (var sid in departingAerodrome.Sids)
                            {
                                if (sid.DepartureCode == sidPoint.Point.Code ||
                                    sid.Transitions.Any(t => t.Code == sidPoint.Point.Code))
                                    result.AvailableSids.First(s => s.Name == sid.Name).RouteWithSid =
                                        sid.Code + "/" + (sid.Runways.Contains('|') ? "<RWY>" : sid.Runways) + " " + 
                                        sidPoint.Point.Code + " " + sidPoint.Airway.Code + " " + string.Join(' ', segments[(i)..]);
                            }
                            
                            sidPoint = reverseTravel ? sidPoint.LastLeg.StartPoint : sidPoint.NextLeg.EndPoint;
                        } while (sidPoint.Point.Code != point.Point.Code);
                    }

                    if (segmentResult.State != ValidationState.INVALID)
                    {
                        var airwayPoints = new List<Location>();
                        do
                        {
                            airwayPoints.Add(point.Point.Location);
                            point = reverseTravel ? point.NextLeg.EndPoint : point.LastLeg.StartPoint;
                        } while (point.Point.Code != entryPoint.Point.Code);

                        airwayPoints.Reverse();
                        routeMap.AddRange(airwayPoints);
                    }
                }

                lastCodepoint = segment;
            }
            else
            {
                if (segment == "DCT")
                {
                    entryPoint = null;
                }
                else
                {
                    var airway = _airwayService.GetAirway(segment);
                    if (airway == null)
                    {
                        segmentResult.State = ValidationState.INVALID;
                        segmentResult.StateDetails = "Could not find airway";
                        waypoints.Last().Type = WaypointType.ERROR;
                        invalid = true;
                    }
                    else
                    {
                        entryPoint = airway.GetPoint(lastCodepoint!);
                        if (entryPoint == null)
                        {
                            segmentResult.State = ValidationState.INVALID;
                            segmentResult.StateDetails = "Cannot enter airway from this location";
                            waypoints.Last().Type = WaypointType.ERROR;
                            invalid = true;
                        }
                    }
                }
            }

            waypoint = !waypoint;

            segmentResults.Add(segmentResult);
        }

        if (international)
            result.DestinationAirport = new PlanSegmentValidationResult()
            {
                Segment = plan.DestinationAirport,
                State = ValidationState.INTERNATIONAL
            };         
        else
        {
            if (destinationAerodrome == null)
                result.DestinationAirport = new PlanSegmentValidationResult()
                {
                    Segment = plan.DestinationAirport,
                    StateDetails = "Could not find aerodrome",
                    State = ValidationState.INVALID
                };
            else
            {
                result.DestinationAirport = new PlanSegmentValidationResult()
                {
                    Segment = plan.DestinationAirport,
                };
                
                if(segments.Length < 1)
                {
                    var segmentResult = waypoint ? segmentResults[^2] : segmentResults.Last();
                    if (segmentResult.State == ValidationState.VALID &&
                        !destinationAerodrome.PointHasStar(waypoints.Last().Code) &&
                        waypoints.Last().Location.GetDistance(destinationAerodrome.Location) > 50)
                    {
                        waypoints.Last().Type = WaypointType.WARNING;
                        segmentResult.State = ValidationState.WARNING;
                        segmentResult.StateDetails = "Final waypoint is greater than 50NM from destination airport";
                    }
                }
            
                waypoints.Add(new PlanWaypoint()
                {
                    Code = plan.DestinationAirport,
                    Location = destinationAerodrome.Location,
                    Type = WaypointType.AIRPORT
                });
                
                if(!invalid)
                    routeMap.Add(destinationAerodrome.Location);
            }
        }

        result.SegmentValidationResults = segmentResults.ToArray();
        result.Waypoints = waypoints.ToArray();
        result.RouteMap = routeMap.ToArray();

        return result;
    }
}