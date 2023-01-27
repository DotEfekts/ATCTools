using ATCTools.Shared.Models;

namespace ATCTools.Server.Models;

public class Aerodrome : ICodePoint
{
    public Aerodrome(string code, string name, State state, Location location, string? sop = null, string? parent = null)
    {
        Code = code;
        Name = name;
        State = state;
        Location = location;
        SoP = sop;
        Parent = parent;
        AirwayPoints = new List<AirwayPoint>();
        Charts = new List<AerodromeChart>();
        Sids = new List<AerodromeSid>();
        Stars = new List<AerodromeStar>();
    }
    
    public string Code { get; }
    public string Name { get; }
    public State State { get; }
    public Location Location { get; }
    public string? SoP { get; }
    public string? Parent { get; }

    public List<AirwayPoint> AirwayPoints { get; }
    public List<AerodromeChart> Charts { get; }
    public List<AerodromeSid> Sids { get; }
    public List<AerodromeStar> Stars { get; }
}