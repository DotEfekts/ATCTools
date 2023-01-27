namespace ATCTools.Client.Models;

public struct AirportInformation
{
    public string IataCode { get; set; }
    public string IcaoCode { get; set; }
    public string AirportName { get; set; }
    public string Location { get; set; }
}