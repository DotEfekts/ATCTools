using ATCTools.Shared.Models;

namespace ATCTools.Server.Models;

public class AirwaySearch
{
    public AirwaySearch(
        AirwaySearch? parent, 
        ICodePoint point, 
        string pointAirwayCode, 
        string lastAirway, 
        int airways,
        double currentDistance, 
        double totalDistance)
    {
        Parent = parent;
        Point = point;
        PointAirwayCode = pointAirwayCode;
        Airways = airways;
        LastAirway = lastAirway;
        CurrentDistance = currentDistance;
        EstimatedTotalDistance = totalDistance;
    }
    
    public AirwaySearch? Parent { get; set; }
    public ICodePoint Point { get; }
    public string PointAirwayCode { get; set; }
    public string LastAirway { get; }
    public int Airways { get; }
    public double CurrentDistance { get; }
    public double EstimatedTotalDistance { get; }
}