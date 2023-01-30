namespace ATCTools.Server.Models;

public class RouteLegSearch
{
    public Airway? Airway { get; set; }
    public AirwayLeg? AirwayLeg { get; set; }
    public AirwayPoint? AirwayPoint { get; set; }
    public ICodePoint Point { get; set; }
}