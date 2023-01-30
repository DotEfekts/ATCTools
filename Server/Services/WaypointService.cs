using System.Xml;
using ATCTools.Server.Helpers;
using ATCTools.Server.Models;
using ATCTools.Shared.Models;

namespace ATCTools.Server.Services;

public class WaypointService
{
    private readonly Dictionary<string, Waypoint> _waypoints = new();

    private readonly string SCHEMA_PATH = "XmlSchemas" + Path.DirectorySeparatorChar + "Waypoints.xsd";

    public WaypointService(ILogger<WaypointService> logger, IConfiguration configuration)
    {
        using var schemaReader = XmlReader.Create(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + SCHEMA_PATH);
        using var dataReader = XmlReader.Create(configuration["DataFiles:Waypoints"].Replace("~", Directory.GetCurrentDirectory()));
        
        XmlDocument data = new();
        data.Schemas.Add(null, schemaReader);
        data.Load(dataReader);

        try
        {
            data.Validate(null);
                
            var nodes = data.DocumentElement?.SelectNodes("waypoint");

            if (nodes == null)
            {
                logger.LogWarning("No waypoints present in the given XML file");
                return;
            }

            foreach (XmlNode node in nodes)
            {
                var locNode = node.SelectSingleNode("location");
                _waypoints.Add(node.Attributes!["name"]!.Value.ToUpper(),
                    new Waypoint(
                        node.Attributes!["name"]!.Value,
                        new Location 
                        {
                            Latitude = ModelUtilities.GetCoordinate(locNode!.Attributes!["lat"]!.Value) ?? 0,
                            Longitude = ModelUtilities.GetCoordinate(locNode.Attributes!["long"]!.Value) ?? 0
                        }
                    ));
            }
        }
        catch (Exception)
        {
            logger.LogWarning("Given waypoints XML file is invalid or malformed");
        }
    }

    public Waypoint? GetWaypoint(string code)
    {
        if(_waypoints.TryGetValue(code.ToUpper(), out var waypoint))
            return waypoint;
        return null;
    }
    
    public IEnumerable<Waypoint> GetWaypointsNear(Location location, int range)
    {
        var degRange = range * 0.01668468468;

        var x1 = location.Longitude - degRange;
        var x2 = location.Longitude + degRange;
        var y1 = location.Latitude - degRange;
        var y2 = location.Latitude + degRange;

        return _waypoints.Values.Where(n =>
            x1 <= n.Location.Longitude &&
            x2 >= n.Location.Longitude &&
            y1 <= n.Location.Latitude &&
            y2 >= n.Location.Latitude);
    }
}