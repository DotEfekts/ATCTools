using ATCTools.Shared.Models;

namespace ATCTools.Server.Models;

public interface ICodePoint
{
    string Code { get; }
    Location Location { get; }
    List<AirwayPoint> AirwayPoints { get; }
}