using ATCTools.Shared.Models;

namespace ATCTools.Server.Models;

public class StarTransition
{
    public string Code { get; set; }
    public ICodePoint Point { get; set; }
    public string? Runways { get; set; }
    public AircraftType AircraftType { get; set; }
}
