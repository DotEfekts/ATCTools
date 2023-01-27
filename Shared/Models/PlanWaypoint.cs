namespace ATCTools.Shared.Models;

public class PlanWaypoint
{
    public Location Location { get; set; }
    public string Code { get; set; }
    public WaypointType Type { get; set; }
}

public enum WaypointType
{
    STANDARD, AIRSPACE_BORDER, AIRPORT, WARNING, ERROR
}