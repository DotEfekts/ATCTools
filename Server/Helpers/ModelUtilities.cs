using System.Text.RegularExpressions;
using ATCTools.Server.Models;
using ATCTools.Shared.Models;

namespace ATCTools.Server.Helpers;

public static class ModelUtilities
{
    public static State GetState(string state)
        => state switch
        {
            "NSW" => State.NEW_SOUTH_WALES,
            "VIC" => State.VICTORIA,
            "QLD" => State.QUEENSLAND,
            "WA" => State.WESTERN_AUSTRALIA,
            "SA" => State.SOUTH_AUSTRALIA,
            "TAS" => State.TASMANIA,
            "ACT" => State.AUSTRALIAN_CAPITAL_TERRITORY,
            "NT" => State.NORTHERN_TERRITORY,
            "OTH" => State.OTHER,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

    private static readonly Regex CoordRegex = new Regex(@"^(\d{2,3})(\d{2})(\d{2})(.\d)?(W|E|N|S)$");
    public static double? GetCoordinate(string locString)
    {
        var match = CoordRegex.Match(locString);
        
        if (!match.Success)
            return null;

        var hemisphere = match.Groups[5].Value;
        var multiplier = hemisphere is "S" or "W" ? -1 : 1;

        var degrees = double.Parse(match.Groups[1].Value);
        var minutes = double.Parse(match.Groups[2].Value) / 60;
        var seconds = double.Parse(match.Groups[3].Value + (match.Groups[4].Success ? match.Groups[4].Value : "")) / 3600;

        return (degrees + minutes + seconds) * multiplier;
    }

    public static NavaidType GetNavaidType(string type)
        => type switch
        {
            "DME" => NavaidType.DME, 
            "GP" => NavaidType.GP, 
            "ILS" => NavaidType.ILS, 
            "LOC" => NavaidType.LOC, 
            "VOR" => NavaidType.VOR, 
            "NDB" => NavaidType.NDB, 
            "MM" => NavaidType.MM, 
            "OM" => NavaidType.OM, 
            "TAC" => NavaidType.TAC, 
            "GBAS" => NavaidType.GBAS,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static AirwayLegLevel GetLegLevel(string? level)
        => level switch
        {
            "H" => AirwayLegLevel.HIGH, 
            "L" => AirwayLegLevel.LOW, 
            "B" => AirwayLegLevel.BOTH, 
            _ => AirwayLegLevel.BOTH
        };
    
    public static AircraftType GetAircraftType(string? type)
        => type switch
        {
            "j" => AircraftType.JET,
            "p" => AircraftType.NON_JET,
            "b" => AircraftType.BOTH,
            _ => AircraftType.BOTH
        };
    
    public static TransitionType GetTransitionType(string type)
        => type switch
        {
            "nav" => TransitionType.NAV,
            "radar" => TransitionType.RADAR,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}