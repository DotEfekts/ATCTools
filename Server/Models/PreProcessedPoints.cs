namespace ATCTools.Server.Models;

public struct PreProcessedPoints
{
    public PreProcessedPoints(ICodePoint point, double? distance, string? level, int? trackIn, int? trackOut, int? lsaltIn, int? lsaltOut)
    {
        Point = point;
        Distance = distance;
        Level = level;
        TrackIn = trackIn;
        TrackOut = trackOut;
        LsaltIn = lsaltIn;
        LsaltOut = lsaltOut;
    }
    
    public ICodePoint Point { get; }
    public double? Distance { get; }
    public string? Level { get; }
    public int? TrackIn { get; }
    public int? TrackOut { get; }
    public int? LsaltIn { get; }
    public int? LsaltOut { get; }
}