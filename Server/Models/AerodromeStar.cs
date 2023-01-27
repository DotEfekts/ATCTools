using ATCTools.Shared.Models;

namespace ATCTools.Server.Models;

public class AerodromeStar
{
    public string Name { get; set; }
    public string Code { get; set; }
    public ICodePoint Point { get; set; }
    public string Runways { get; set; }
    public AircraftType AircraftType { get; set; }
    public string ArrivalCode { get; set; }
    public List<StarTransition> Transitions { get; set; } = new List<StarTransition>();
}