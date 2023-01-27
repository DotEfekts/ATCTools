using ATCTools.Shared.Models;

namespace ATCTools.Server.Models;

public class AerodromeSid
{
    public string Name { get; set; }
    public string Code { get; set; }
    public ICodePoint? Point { get; set; }
    public bool Radar { get; set; }
    public string Runways { get; set; }
    public AircraftType AircraftType { get; set; }
    public DepartureType DepartureType { get; set; }
    public string? DepartureCode { get; set; }
    public List<SidTransition> Transitions { get; set; } = new List<SidTransition>();
}

public enum DepartureType
{
    NAV, RADAR
}