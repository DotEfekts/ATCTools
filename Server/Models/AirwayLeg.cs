using ATCTools.Server.Helpers;

namespace ATCTools.Server.Models;

public class AirwayLeg
{
    public AirwayLeg(Airway airway, AirwayPoint lastPoint, int? trackIn, Queue<PreProcessedPoints> processedPoints)
    {
        var nextPoint = processedPoints.Dequeue();

        Airway = airway;
        Level = ModelUtilities.GetLegLevel(nextPoint.Level);
        
        if (!trackIn.HasValue)
            throw new ArgumentException("Expecting track in on leg starting at " + lastPoint.Point.Code);
        if (!nextPoint.TrackIn.HasValue)
            throw new ArgumentException("Expecting track out on leg ending at " + nextPoint.Point.Code);
        if (!nextPoint.Distance.HasValue)
            throw new ArgumentException("Expecting distance on leg ending at " + nextPoint.Point.Code);
        
        TrackIn = trackIn.Value;
        TrackOut = nextPoint.TrackIn.Value;
        Distance = nextPoint.Distance.Value;

        StartPoint = lastPoint;
        EndPoint = new AirwayPoint(airway, this, nextPoint, processedPoints);
    }
    
    public Airway Airway { get; }
    public AirwayPoint StartPoint { get; }
    public AirwayPoint EndPoint { get; }
    public AirwayLegLevel Level { get; }
    public int TrackIn { get; }
    public int TrackOut { get; }
    public double Distance { get; }
}

public enum AirwayLegLevel
{
    HIGH, LOW, BOTH
}