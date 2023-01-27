using System.Xml;
using ATCTools.Server.Helpers;
using ATCTools.Server.Models;
using ATCTools.Shared.Models;

namespace ATCTools.Server.Services;

public class NavaidService
{
    private readonly Dictionary<string, Navaid> _navaids = new();
    private readonly Dictionary<string, List<Navaid>> _locNavaids = new();

    private readonly string SCHEMA_PATH = "XmlSchemas" + Path.DirectorySeparatorChar + "Navaids.xsd";

    public NavaidService(ILogger<NavaidService> logger, IConfiguration configuration)
    {
        using var schemaReader = XmlReader.Create(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + SCHEMA_PATH);
        using var dataReader = XmlReader.Create(configuration["DataFiles:Navaids"].Replace("~", Directory.GetCurrentDirectory()));
        
        XmlDocument data = new();
        data.Schemas.Add(null, schemaReader);
        data.Load(dataReader);

        try
        {
            data.Validate(null);
                
            var nodes = data.DocumentElement?.SelectNodes("navaid");

            if (nodes == null)
            {
                logger.LogWarning("No navaids present in the given XML file");
                return;
            }

            foreach (XmlNode node in nodes)
            {
                var locNode = node.SelectSingleNode("location");
                var code = node.Attributes!["code"]!.Value;

                var navaid = new Navaid(
                    code,
                    node.Attributes!["name"]!.Value,
                    ModelUtilities.GetNavaidType(node.Attributes!["type"]!.Value),
                    new Location 
                    {
                        Latitude = ModelUtilities.GetCoordinate(locNode!.Attributes!["lat"]!.Value) ?? 0,
                        Longitude = ModelUtilities.GetCoordinate(locNode.Attributes!["long"]!.Value) ?? 0
                    }
                );
                
                _navaids.Add(code.ToUpper() + " " + node.Attributes!["type"]!.Value.ToUpper(),
                    navaid);
                
                if(!_locNavaids.ContainsKey(code.ToUpper()))
                    _locNavaids.Add(code.ToUpper(), new List<Navaid>());
                _locNavaids[code.ToUpper()].Add(navaid);
            }
        }
        catch (Exception)
        {
            logger.LogWarning("Given navaid XML file is invalid or malformed");
        }
    }

    public Navaid? GetNavaid(string code)
    {
        if(_navaids.TryGetValue(code.ToUpper(), out var navaid))
            return navaid;
        return null;
    }

    public IEnumerable<Navaid> GetNavaids(string code)
    {
        if(_locNavaids.TryGetValue(code.ToUpper(), out var navaids))
            return navaids.ToArray();
        return Array.Empty<Navaid>();
    }
    
    
    public IEnumerable<Navaid> GetNavaidsNear(Location location)
    {
        var x1 = location.Longitude - 0.05;
        var x2 = location.Longitude + 0.05;
        var y1 = location.Latitude - 0.05;
        var y2 = location.Latitude + 0.05;

        return _navaids.Values.Where(n =>
            x1 <= n.Location.Longitude &&
            x2 >= n.Location.Longitude &&
            y1 <= n.Location.Latitude &&
            y2 >= n.Location.Latitude);
    }
}