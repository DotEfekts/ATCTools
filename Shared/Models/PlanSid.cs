namespace ATCTools.Shared.Models;

public class PlanSid
{
    public string Name { get; set; }
    public bool IsRadar { get; set; }
    public AircraftType AircraftType { get; set; }
    public string Runways { get; set; }
    public string Transitions { get; set; }
    public string? RouteWithSid { get; set; }
}