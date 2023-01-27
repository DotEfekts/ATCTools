using System.Xml;
using ATCTools.Server.Helpers;
using ATCTools.Server.Models;
using ATCTools.Shared.Models;

namespace ATCTools.Server.Services;

public class CodepointService
{
    private readonly AerodromeService _aerodromeService;
    private readonly NavaidService _navaidService;
    private readonly WaypointService _waypointService;
    
    public CodepointService(
        AerodromeService aerodromeService,
        NavaidService navaidService,
        WaypointService waypointService
        )
    {
        _aerodromeService = aerodromeService;
        _navaidService = navaidService;
        _waypointService = waypointService;

        _aerodromeService.InjectSidStartPoints(this);
    }

    public ICodePoint? GetCodepoint(string code)
    {
        ICodePoint? codepoint = null;
        if (code.Contains(' '))
        {
            if (code.EndsWith(" AD") || code.EndsWith(" ALA") || code.EndsWith(" HLS"))
            {
                codepoint = _aerodromeService.GetAerodrome(code.Split(' ')[0]);
            }
            else
            {
                codepoint = _navaidService.GetNavaid(code);
            }
        }
        
        codepoint ??= _waypointService.GetWaypoint(code);

        if (codepoint == null)
        {
            var vor = _navaidService.GetNavaid(code + " VOR");
            var ndb = _navaidService.GetNavaid(code + " NDB");
            var dme = _navaidService.GetNavaid(code + " DME");

            codepoint = (ndb?.AirwayPoints.Count ?? 0) > (vor?.AirwayPoints.Count ?? 0) ? ndb : (dme?.AirwayPoints.Count ?? 0) > (vor?.AirwayPoints.Count ?? 0) ? dme : vor;
            codepoint ??= vor ?? ndb ?? dme;
        }

        return codepoint;
    }

    public IEnumerable<ICodePoint> GetCodepointNear(Location location)
    {
        var points = new List<ICodePoint>();
        
        points.AddRange(_aerodromeService.GetAerodromesNear(location).Select(c => (ICodePoint) c));
        points.AddRange(_waypointService.GetWaypointsNear(location).Select(c => (ICodePoint) c));
        points.AddRange(_navaidService.GetNavaidsNear(location).Select(c => (ICodePoint) c));

        return points;
    }
}