namespace ATCTools.Server.Models;

public class AirwayPoint
{
    public AirwayPoint(Airway airway, AirwayLeg? lastLeg, PreProcessedPoints point, Queue<PreProcessedPoints> processedPoints)
    {
        var queueEmpty = processedPoints.Count == 0;
        Point = point.Point;
        Airway = airway;
        
        if (lastLeg == null)
            Type = point.TrackIn != null ? AirwayPointType.START_INTERNATIONAL : AirwayPointType.START;
        else if (queueEmpty)
            Type = point.TrackOut != null ? AirwayPointType.END_INTERNATIONAL : AirwayPointType.END;
        else
            Type = AirwayPointType.TRANSITORY;
        
        LsaltIn = point.LsaltIn;
        LsaltOut = point.LsaltIn;

        LastLeg = lastLeg;
        NextLeg = queueEmpty ? null : new AirwayLeg(airway, this, point.TrackOut, processedPoints);
        Point.AirwayPoints.Add(this);
    }
    
    public ICodePoint Point { get; }
    public AirwayPointType Type { get; }
    public Airway Airway { get; }
    public AirwayLeg? LastLeg { get; }
    public AirwayLeg? NextLeg { get; }

    public int? LsaltIn { get; }
    public int? LsaltOut { get; }
}

public enum AirwayPointType {
    START, START_INTERNATIONAL, END, END_INTERNATIONAL, TRANSITORY
}