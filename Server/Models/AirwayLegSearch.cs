namespace ATCTools.Server.Models;

public class AirwayLegSearch
{
    public Airway Airway { get; set; }
    public AirwayPoint AirwayPoint { get; set; }
    public AirwayLeg AirwayLeg { get; set; }
    public bool High { get; set; }
}