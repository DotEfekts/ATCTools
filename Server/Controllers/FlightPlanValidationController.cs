using System.Text.RegularExpressions;
using ATCTools.Server.Models;
using ATCTools.Server.Services;
using Microsoft.AspNetCore.Mvc;
using ATCTools.Shared.Models;

namespace ATCTools.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class FlightPlanValidationController
{
    private readonly AirwayService _airwayService;
    private readonly AerodromeService _aerodromeService;
    private readonly CodepointService _codepointService;
    
    private static readonly Regex CoordRegex = new Regex(@"^(\d{2})(\d{2})?(\d{2})?(N|S)(\d{3})(\d{2})?(\d{2})?(E|W)$");
    private static readonly Regex LevelAndSpeedRegex = new Regex(@"^((N|K)(\d{4})|M(\d{3}))((F|A)(\d{3})|(S|M)(\d{4}))$");
    private static readonly Regex AirwayCollapseRegex = new Regex(@"^(.*?)<AWC>([^ ]+) ([^ /]+) <AWC>([^ ]+)(.*)$");
    
    public FlightPlanValidationController(
        AirwayService airwayService,
        AerodromeService aerodromeService,
        CodepointService codepointService)
    {
        _airwayService = airwayService;
        _aerodromeService = aerodromeService;
        _codepointService = codepointService;
    }

    [HttpPost]
    public PlanValidationResult CheckSegmentValidity(FlightPlan plan)
    {
        var segments = plan.Route.ToUpper().Split(' ');
        plan.DepartingAirport = plan.DepartingAirport.ToUpper();
        plan.DestinationAirport = plan.DestinationAirport.ToUpper();

        var segmentResults = new List<PlanSegmentValidation>();

        var departingAerodrome = _aerodromeService.GetAerodrome(plan.DepartingAirport);
        var destinationAerodrome = _aerodromeService.GetAerodrome(plan.DestinationAirport);

        foreach (var x in segments)
        {
            if(string.IsNullOrWhiteSpace(x))
                continue;
            
            var segment = x;
            string? secondaryInfo = null;

            PlanSegmentValidation? segmentResult = null;

            var last = segmentResults.LastOrDefault();
            
            if (segment is "IFR" or "VFR")
            {
                if (last == null)
                {
                    segmentResult = new PlanSegmentValidation
                    {
                        Code = segment,
                        Type = PlanSegmentType.UNKNOWN,
                        State = ValidationState.INVALID,
                        ValidationMessage = "Cannot specify a change in flight rules as first segment"
                    };
                }
                else
                {
                    if (last is DirectPlanSegment or AirwayPlanSegment)
                    {
                        last.State = ValidationState.INVALID;
                        last.ValidationMessage = "Cannot specify a change in flight rules between points";
                    }
                
                    last.ChangeToIFR = segment == "IFR";
                    continue;
                }
            }

            if (segment.Contains('/'))
            {
                var split = segment.Split('/');
                segment = split[0];
                secondaryInfo = split[1];
            }

            if (segment == "DCT")
            {
                segmentResult = new DirectPlanSegment()
                {
                    Code = "DCT",
                    Type = PlanSegmentType.ENTITY
                };
            }

            var codepoint = _codepointService.GetCodepoint(segment);
            if (codepoint != null)
            {
                segmentResult = new CodepointPlanSegment
                {
                    CodePoint = codepoint,
                    Location = codepoint.Location,
                    Type = PlanSegmentType.ENTITY,
                    Code = codepoint.Code
                };

                if (last is AirwayPlanSegment airwaySegment)
                {
                    airwaySegment.Exit = codepoint.AirwayPoints.FirstOrDefault(p => p.Airway == airwaySegment.Airway);
                    airwaySegment.Reverse =
                        Array.IndexOf(airwaySegment.Airway.AirwayPoints, airwaySegment.Entry) >
                        Array.IndexOf(airwaySegment.Airway.AirwayPoints, airwaySegment.Exit);
                }
            }

            var aerodrome = _aerodromeService.GetAerodrome(segment);
            if (aerodrome != null)
            {
                segmentResult = new AerodromePlanSegment
                {
                    Aerodrome = aerodrome,
                    Location = aerodrome.Location,
                    Type = PlanSegmentType.ENTITY,
                    Code = aerodrome.Code
                };
            }

            var airway = _airwayService.GetAirway(segment);
            if (airway != null)
            {
                AirwayPoint? entry = null;
                if (last is CodepointPlanSegment codepointSegment)
                {
                    entry = codepointSegment.CodePoint.AirwayPoints.FirstOrDefault(p => p.Airway == airway);
                }
                
                segmentResult = new AirwayPlanSegment
                {
                    Entry = entry,
                    Airway = airway,
                    Type = PlanSegmentType.ENTITY,
                    Code = airway.Code
                };
            }

            var sid = departingAerodrome?.Sids.FirstOrDefault(s => s.Code == segment);
            if (sid != null)
            {
                segmentResult = new SidPlanSegment
                {
                    Sid = sid,
                    SelectedRunway = secondaryInfo,
                    Type = PlanSegmentType.ENTITY,
                    Code = sid.Code
                };
            }

            var star = destinationAerodrome?.Stars.FirstOrDefault(s => s.Code == segment);
            if (star != null)
            {
                segmentResult = new StarPlanSegment
                {
                    Star = star,
                    SelectedRunway = secondaryInfo,
                    Type = PlanSegmentType.ENTITY,
                    Code = star.Code
                };
            }

            var coordinate = CoordRegex.Match(segment);
            if (coordinate.Success)
            {
                var latMultiplier = coordinate.Groups[4].Value == "S" ? -1 : 1;
                var lonMultiplier = coordinate.Groups[8].Value == "W" ? -1 : 1;

                var lat = double.Parse(coordinate.Groups[1].Value);
                lat += coordinate.Groups[2].Success ? double.Parse(coordinate.Groups[2].Value) / 60 : 0;
                lat += coordinate.Groups[3].Success ? double.Parse(coordinate.Groups[3].Value) / 3600 : 0;
                
                var lon = double.Parse(coordinate.Groups[5].Value);
                lon += coordinate.Groups[6].Success ? double.Parse(coordinate.Groups[6].Value) / 60 : 0;
                lon += coordinate.Groups[7].Success ? double.Parse(coordinate.Groups[7].Value) / 3600 : 0;

                segmentResult = new PlanSegmentValidation
                {
                    Code = segment,
                    Location = new Location
                    {
                        Latitude = lat * latMultiplier,
                        Longitude = lon * lonMultiplier
                    },
                    Type = PlanSegmentType.COORDINATE
                };
            }

            segmentResult ??= new PlanSegmentValidation
            {
                Code = segment,
                Type = PlanSegmentType.UNKNOWN,
                State = ValidationState.INVALID,
                ValidationMessage = "Unable to detect segment type"
            };

            if (secondaryInfo != null && segmentResult is not (SidPlanSegment or StarPlanSegment))
            {
                var match = LevelAndSpeedRegex.Match(secondaryInfo);
                if (match.Success)
                {
                    if (segmentResult is DirectPlanSegment || segmentResult is AirwayPlanSegment)
                    {
                        segmentResult.State = ValidationState.INVALID;
                        segmentResult.ValidationMessage = "Cannot specify a change in level and speed between points";
                    }
                    
                    segmentResult.FlightLevelChange = match.Groups[1].Value;
                    segmentResult.SpeedChange = match.Groups[5].Value;
                }
                else
                {
                    segmentResult.State = ValidationState.INVALID;
                    segmentResult.ValidationMessage = "Unable to parse secondary information";
                }
            }

            segmentResults.Add(segmentResult);
        }
        
        foreach (var (segment, i) in segmentResults.Select((value, i) => (value, i)))
        {
            var lastIndex = segmentResults.Count - 1;
            var lastSegment = i > 0 ? segmentResults[i - 1] : null;
            var nextSegment = i < lastIndex ? segmentResults[i + 1] : null;

            if (segment.Type == PlanSegmentType.ENTITY)
            {
                if (segment is AerodromePlanSegment aerodrome)
                {
                    if (i == 0 && plan.DepartingAirport != aerodrome.Code)
                    {
                        segment.State = ValidationState.WARNING;
                        segment.ValidationMessage = "Specified departure aerodrome is different from code in flight plan";
                        continue;
                    }
                    
                    if (i == lastIndex && plan.DestinationAirport != aerodrome.Code)
                    {
                        segment.State = ValidationState.WARNING;
                        segment.ValidationMessage = "Specified destination aerodrome is different from code in flight plan";
                        continue;
                    }
                    
                    if(i > 0 && i < lastIndex)
                    {
                        segment.State = ValidationState.INVALID;
                        segment.ValidationMessage = "You cannot enter an aerodrome code here";
                        continue;
                    }
                }
                else if (segment is SidPlanSegment sid)
                {
                    if (i != 0 && !(i == 1 && lastSegment is AerodromePlanSegment))
                    {
                        segment.State = ValidationState.INVALID;
                        segment.ValidationMessage = "You cannot enter a SID here";
                        continue;
                    }

                    var rws = sid.Sid.Runways.Split('|');

                    if (rws.All(r => r != sid.SelectedRunway))
                    {
                        segment.State = ValidationState.INVALID;
                        segment.ValidationMessage = "Invalid runway specified for selected SID";
                        continue;
                    }
                }
                else if (segment is StarPlanSegment star)
                {
                    if (i != lastIndex && !(i == lastIndex - 1 && nextSegment is AerodromePlanSegment))
                    {
                        segment.State = ValidationState.INVALID;
                        segment.ValidationMessage = "You cannot enter a STAR here";
                        continue;
                    }

                    var waypoint = lastSegment?.Code;
                    var transition = star.Star.Transitions.FirstOrDefault(t => t.Code == waypoint);
                    
                    var rws = (transition?.Runways ?? star.Star.Runways).Split('|');

                    if (rws.All(r => r != star.SelectedRunway))
                    {
                        segment.State = ValidationState.INVALID;
                        segment.ValidationMessage = "Invalid runway specified for selected STAR" + (transition?.Runways != null ? " transition" : "");
                        continue;
                    }
                }
                else if (segment is CodepointPlanSegment codepoint)
                {
                    if (i > 0)
                    {
                        if(lastSegment is SidPlanSegment { Sid.Radar: false } sidSegment)
                        {
                            var waypoint = codepoint.Code;
                            var transitions = sidSegment.Sid.Transitions.Select(t => t.Code).Concat(new[] { sidSegment.Sid.DepartureCode });

                            if (transitions.All(t => t != waypoint))
                            {
                                var radarTransition =
                                    sidSegment.Sid.Transitions.FirstOrDefault(t => t.Type == TransitionType.RADAR);

                                if (radarTransition == null)
                                {
                                    segment.State = ValidationState.INVALID;
                                    segment.ValidationMessage = "Invalid waypoint specified for selected SID";
                                    continue;
                                }

                                if (radarTransition.Track != null && sidSegment.Sid.Point != null)
                                {
                                    var track = (int) radarTransition.Track!;
                                    var departurePoint = sidSegment.Sid.Point;
                                    var bearingToWaypoint = departurePoint!.Location.GetBearingTo(codepoint.Location!);
                                    var difference = (bearingToWaypoint - track + 540) % 360 - 180;
                                    if (Math.Abs(difference) > 90)
                                    {
                                        segment.State = ValidationState.WARNING;
                                        segment.ValidationMessage = "Waypoint after radar transition is behind aircraft";
                                        continue;
                                    }
                                }
                            }
                        }

                        if (lastSegment is AirwayPlanSegment { Exit: null } airwaySegment)
                        {
                            if (airwaySegment.Airway.AirwayPoints[^1].Type == AirwayPointType.END_INTERNATIONAL ||
                                (airwaySegment.Airway.TwoWay && airwaySegment.Airway.AirwayPoints[0].Type ==
                                    AirwayPointType.START_INTERNATIONAL))
                            {
                                segment.State = ValidationState.INTERNATIONAL;
                                segment.ValidationMessage = "Flight plan believed to exit country airspace";
                                continue;
                            }

                            segment.State = ValidationState.INVALID;
                            segment.ValidationMessage = "Invalid waypoint exit specified for selected airway";
                            continue;
                        }

                        if (lastSegment is AirwayPlanSegment { Reverse: true, Airway.TwoWay: false })
                        {
                            segment.State = ValidationState.INVALID;
                            segment.ValidationMessage = "Invalid direction for selected airway";
                            continue;
                        }

                        if (lastSegment is CodepointPlanSegment || lastSegment?.Type == PlanSegmentType.COORDINATE)
                        {
                            segment.State = ValidationState.INVALID;
                            segment.ValidationMessage = "Please specify an airway or DCT between waypoints";
                            continue;
                        }
                    }

                    if (i < lastIndex)
                    {
                        if (lastSegment is StarPlanSegment starSegment)
                        {
                            var selectedTransition = starSegment.Star.Transitions.FirstOrDefault(t => t.Code == codepoint.Code);

                            if (selectedTransition == null && starSegment.Star.ArrivalCode != codepoint.Code)
                            {
                                segment.State = ValidationState.INVALID;
                                segment.ValidationMessage = "Invalid waypoint specified for selected STAR";
                                continue;
                            }
                        }
                    }
                }
                else if (segment is AirwayPlanSegment airway)
                {
                    if (i == 0)
                    {
                        segment.State = ValidationState.INVALID;
                        segment.ValidationMessage = "You cannot specify an airway as the first segment";
                        continue;
                    }

                    if (lastSegment is DirectPlanSegment or AirwayPlanSegment)
                    {
                        segment.State = ValidationState.INVALID;
                        segment.ValidationMessage = "You cannot specify two travel paths in succession";
                        continue;
                    }
                    
                    if (airway.Entry == null)
                    {
                        segment.State = ValidationState.INVALID;
                        segment.ValidationMessage = "Invalid waypoint entry specified for selected airway";
                        continue;
                    }
                }
                else if (segment is DirectPlanSegment direct)
                {
                    if (lastSegment is DirectPlanSegment or AirwayPlanSegment)
                    {
                        segment.State = ValidationState.INVALID;
                        segment.ValidationMessage = "You cannot specify two travel paths in succession";
                        continue;
                    }

                    if (lastSegment is CodepointPlanSegment lastCodepoint && nextSegment is CodepointPlanSegment nextCodepoint)
                    {
                        var exitAirways = nextCodepoint.CodePoint.AirwayPoints.Select(p => p.Airway);
                        direct.AirwayAlternate = lastCodepoint.CodePoint.AirwayPoints
                            .FirstOrDefault(p => exitAirways.Contains(p.Airway))?.Airway;
                    }
                }
                else
                {
                    segment.State = ValidationState.INVALID;
                    segment.ValidationMessage = "Unknown plan validation entity type (" + segment.GetType().Name +")";
                    continue;
                }
            }
        }

        var departingResult = new PlanSegmentValidationResult
        {
            Segment = departingAerodrome?.Code ?? plan.DepartingAirport,
            State = departingAerodrome != null ? ValidationState.VALID : ValidationState.INVALID,
            Location = departingAerodrome?.Location,
            StateDetails = departingAerodrome == null ? "Could not find departing aerodrome" : ""
        };

        var sids = new List<PlanSid>();
        if (departingAerodrome != null)
        {
            List<string> points = segmentResults.OfType<CodepointPlanSegment>().Select(c => c.Code).ToList();
            var sidPoints = departingAerodrome.Sids
                .Select(s => new { Sid = s, Point = s.Transitions.Select( t => t.Code)
                .Concat(new [] {s.DepartureCode }).FirstOrDefault(p => p != null && points.Contains(p)) }).ToList();
            
            var preSid = segmentResults.First() is AerodromePlanSegment aerodrome ? aerodrome.Code + " " : "";
            
            foreach (var val in sidPoints)
            {
                var sid = val.Sid;
                
                string? routeWithSid = null;
                if (val.Point != null)
                {
                    var startIndex = segmentResults.FindIndex(s => s.Code == val.Point);
                    routeWithSid = preSid + sid.Code + "/" + sid.Runways + " " +
                                   string.Join(' ', segmentResults.Skip(startIndex).Select(s => s.RebuildSegment()));
                }

                if (val.Sid.Radar)
                {
                    var startIndex = segmentResults.FindIndex(s => s is not AerodromePlanSegment && s is not SidPlanSegment);
                    routeWithSid = preSid + sid.Code + "/" + sid.Runways + " " +
                                   string.Join(' ', segmentResults.Skip(startIndex).Select(s => s.RebuildSegment()));
                }
                
                sids.Add(new PlanSid
                {
                    Name = sid.Name,
                    AircraftType = sid.AircraftType,
                    Runways = sid.Runways,
                    IsRadar = sid.Radar,
                    Transitions = string.Join(' ',
                        sid.Transitions.Select(t =>
                            t.Type == TransitionType.NAV ? t.Code : "RADAR" + (t.Track.HasValue ? "-" + t.Track : ""))),
                    RouteWithSid = routeWithSid
                });
            }
        }
        
        var results = new List<PlanSegmentValidationResult>();
        var routeMap = new List<Location>();

        var invalid = false;
        var international = false;
        
        if(departingAerodrome?.Location != null)
            routeMap.Add(departingAerodrome.Location);

        foreach (var segment in segmentResults.Where(s => s is not AerodromePlanSegment))
        {
            var result = new PlanSegmentValidationResult()
            {
                Segment = segment.RebuildSegment(),
                MapCode = segment.Code,
                Location = segment.Location
            };
            results.Add(result);
            
            if (invalid)
            {
                result.State = ValidationState.UNVALIDATED;
                continue;
            }

            if (international)
            {
                result.State = ValidationState.INTERNATIONAL;
                continue;
            }

            if (segment.State != ValidationState.VALID)
            {
                result.State = segment.State;
                result.StateDetails = segment.ValidationMessage;

                if (result.State == ValidationState.INVALID)
                    invalid = true;

                if (result.State == ValidationState.INTERNATIONAL)
                    international = true;
                
                if(result.State != ValidationState.WARNING)
                    continue;
            }
            
            if(segment.Location != null)
                routeMap.Add(segment.Location);

            if (segment is AirwayPlanSegment { Exit: { } } airway)
            {
                var point = airway.Reverse!.Value ? airway.Entry!.LastLeg!.StartPoint : airway.Entry!.NextLeg!.EndPoint;
                while (point != airway.Exit)
                {
                    routeMap.Add(point.Point.Location);
                    point = airway.Reverse!.Value ? point.LastLeg!.StartPoint : point.NextLeg!.EndPoint;
                }
            }
        }

        var destinationResult = new PlanSegmentValidationResult
        {
            Segment = destinationAerodrome?.Code ?? plan.DestinationAirport,
            State = destinationAerodrome != null ? ValidationState.VALID : international ? ValidationState.INTERNATIONAL : ValidationState.INVALID,
            Location = destinationAerodrome?.Location,
            StateDetails = destinationAerodrome == null ? "Could not find departing aerodrome" : ""
        };

        var stars = new List<PlanStar>();
        if (destinationAerodrome != null)
        {
            var points = segmentResults.OfType<CodepointPlanSegment>().Select(c => c.Code).ToList();
            var starPoints = destinationAerodrome.Stars
                .Select(s => new { Star = s, Point = s.Transitions.Select( t => t.Code)
                .Concat(new [] { s.ArrivalCode }).FirstOrDefault(p => points.Contains(p)) }).ToList();
            
            var postStar = segmentResults.Last() is AerodromePlanSegment aerodrome ? " " + aerodrome.Code : "";
            
            foreach (var val in starPoints)
            {
                var star = val.Star;
                
                string? routeWithStar = null;
                if (val.Point != null)
                {
                    var endIndex = segmentResults.FindIndex(s => s.Code == val.Point);
                    var rws = star.Transitions.FirstOrDefault(t => t.Code == val.Point)?.Runways ?? star.Runways;
                    routeWithStar = string.Join(' ', segmentResults.Take(endIndex + 1).Select(s => s.RebuildSegment()))
                                    + " " + star.Code + "/" + rws + postStar;
                }
                
                stars.Add(new PlanStar
                {
                    Name = star.Name,
                    AircraftType = star.AircraftType,
                    Runways = star.Runways,
                    Transitions = string.Join(' ',
                        star.Transitions.Select(t =>
                            t.Code + (t.Runways != null ? " (RWY " + t.Runways +")" : "") + 
                            (t.AircraftType != AircraftType.BOTH ? t.AircraftType == AircraftType.JET ? " (JET)" : " (NON-JET)" : ""))),
                    RouteWithStar = routeWithStar
                });
            }
        }
        
        if(!invalid && destinationAerodrome?.Location != null)
            routeMap.Add(destinationAerodrome.Location);

        string dctCollapse = "";
        foreach (var segment in segmentResults)
            dctCollapse += segment is DirectPlanSegment { AirwayAlternate: { } } direct
                ? " <AWC>" + direct.AirwayAlternate.Code
                : " " + segment.RebuildSegment();
        
        dctCollapse = dctCollapse.TrimStart();
        var preCollapse = dctCollapse.Replace("<AWC>", "");
        
        var awc = AirwayCollapseRegex.Match(dctCollapse);
        while (awc.Success)
        {
            if (awc.Groups[2].Value == awc.Groups[4].Value)
                dctCollapse = awc.Groups[1].Value + "<AWC>" + awc.Groups[2] + awc.Groups[5].Value;
            else
                dctCollapse = awc.Groups[1].Value + awc.Groups[2].Value + " " + awc.Groups[3].Value + " <AWC>" + awc.Groups[4].Value + awc.Groups[5].Value;
            awc = AirwayCollapseRegex.Match(dctCollapse);
        }

        dctCollapse = dctCollapse.Replace("<AWC>", "");
        
        return new PlanValidationResult
        {
            SegmentValidationResults = results.ToArray(),
            DctCollapse = dctCollapse != preCollapse ? dctCollapse : null,
            RouteMap = routeMap.ToArray(),
            DepartingAirport = departingResult,
            DestinationAirport = destinationResult,
            AvailableSids = sids,
            AvailableStars = stars
        };
    }
}