namespace ATCTools.Shared.Models;

public class Location
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    public double GetDistance(Location to)
    {
        var d1 = Latitude * (Math.PI / 180.0);
        var num1 = Longitude * (Math.PI / 180.0);
        var d2 = to.Latitude * (Math.PI / 180.0);
        var num2 = to.Longitude * (Math.PI / 180.0) - num1;
        var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
    
        var distM = 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));

        return distM * 0.0005399568;
    }
    
    public double GetBearingTo(Location location)
    {   
        var dLon = ToRad(location.Longitude-Longitude);
        var dPhi = Math.Log(
            Math.Tan(ToRad(location.Latitude)/2+Math.PI/4)/Math.Tan(ToRad(Latitude)/2+Math.PI/4));
        if (Math.Abs(dLon) > Math.PI) 
            dLon = dLon > 0 ? -(2*Math.PI-dLon) : (2*Math.PI+dLon);
        return ToBearing(Math.Atan2(dLon, dPhi));
    }

    private static double ToRad(double degrees)
        => degrees * (Math.PI / 180);

    private static double ToDegrees(double radians)
        => radians * 180 / Math.PI;

    private static double ToBearing(double radians) 
        => (ToDegrees(radians) +360) % 360;
}