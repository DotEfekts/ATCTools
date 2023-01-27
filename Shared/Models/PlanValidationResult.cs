using System.Numerics;

namespace ATCTools.Shared.Models;

public class PlanValidationResult
{
    public PlanSegmentValidationResult DepartingAirport { get; set; }
    public PlanSegmentValidationResult DestinationAirport { get; set; }
    public PlanSegmentValidationResult FlightLevel { get; set; }
    public PlanSegmentValidationResult[] SegmentValidationResults { get; set; }
    public PlanWaypoint[] Waypoints { get; set; }
    public Location[] RouteMap { get; set; }
    public List<PlanSid> AvailableSids { get; set; }
    public List<PlanStar> AvailableStars { get; set; }
}