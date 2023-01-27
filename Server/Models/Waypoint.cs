using ATCTools.Shared.Models;

namespace ATCTools.Server.Models;

public class Waypoint : ICodePoint
{
    public Waypoint(string code, Location location)
    {
        Code = code;
        Location = location;
        AirwayPoints = new List<AirwayPoint>();
    }
    
    public string Code { get; }
    public Location Location { get; }
    public List<AirwayPoint> AirwayPoints { get; }
}
