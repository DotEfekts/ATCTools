using ATCTools.Shared.Models;

namespace ATCTools.Server.Models;

public class Navaid : ICodePoint
{
    public Navaid(string code, string name, NavaidType type, Location location)
    {
        Code = code;
        Name = name;
        Type = type;
        Location = location;
        AirwayPoints = new List<AirwayPoint>();
    }
    
    public string Code { get; }
    public string Name { get; }
    public NavaidType Type { get; }
    public Location Location { get; }
    public List<AirwayPoint> AirwayPoints { get; }
}

public enum NavaidType {
    DME, GP, ILS, LOC, VOR, NDB, MM, OM, TAC, GBAS, AD, ALA, HLS
}