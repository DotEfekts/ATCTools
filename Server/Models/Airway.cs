namespace ATCTools.Server.Models;

public class Airway
{
    private Dictionary<string, AirwayPoint> _pointCodes { get; }

    public Airway(string code, bool twoWay, bool limitedSegment, Queue<PreProcessedPoints> processedPoints)
    {
        Code = code;
        TwoWay = twoWay;
        LimitedSegment = limitedSegment;

        var points = new List<AirwayPoint>();
        var legs = new List<AirwayLeg>();
        _pointCodes = new Dictionary<string, AirwayPoint>();

        var nextPoint = new AirwayPoint(this, null, processedPoints.Dequeue(), processedPoints);

        while (nextPoint != null)
        {
            points.Add(nextPoint);
            _pointCodes.Add(nextPoint.Point.Code.Split(' ')[0], nextPoint);
            
            if (nextPoint.NextLeg != null)
            {
                legs.Add(nextPoint.NextLeg);
                nextPoint = nextPoint.NextLeg.EndPoint;
            }
            else nextPoint = null;
        }

        AirwayPoints = points.ToArray();
        AirwayLegs = legs.ToArray();
    }

    public string Code { get; }
    public bool TwoWay { get; }
    public bool LimitedSegment { get; }
    public AirwayPoint[] AirwayPoints { get; }
    public AirwayLeg[] AirwayLegs { get; }

    public AirwayPoint? GetPoint(string code)
    {
        if (_pointCodes.ContainsKey(code))
            return _pointCodes[code];
        return null;
    }
}