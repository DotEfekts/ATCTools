namespace ATCTools.Shared.Models;

public class PlanStar
{
    public string Name { get; set; }
    public AircraftType AircraftType { get; set; }
    public string Runways { get; set; }
    public string Transitions { get; set; }
    public string? RouteWithStar { get; set; }
}