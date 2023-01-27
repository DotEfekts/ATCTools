using System.Xml;
using System.Xml.Schema;
using ATCTools.Server.Models;

namespace ATCTools.Server.Services;

public class AirwayService
{
    private readonly Dictionary<string, Airway> _airways = new();

    private readonly string SCHEMA_PATH = "XmlSchemas" + Path.DirectorySeparatorChar + "Airways.xsd";

    public AirwayService(
        ILogger<AirwayService> logger, 
        IConfiguration configuration, 
        CodepointService codepointService)
    {
        using var schemaReader = XmlReader.Create(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + SCHEMA_PATH);
        using var dataReader = XmlReader.Create(configuration["DataFiles:Airways"].Replace("~", Directory.GetCurrentDirectory()));
        
        XmlDocument data = new();
        data.Schemas.Add(null, schemaReader);
        data.Load(dataReader);

        try
        {
            data.Validate(null);
                
            var nodes = data.DocumentElement?.SelectNodes("airway");

            if (nodes == null)
            {
                logger.LogWarning("No airways present in the given XML file");
                return;
            }

            foreach (XmlNode node in nodes)
            {
                var points = node.SelectNodes("waypoint");
                var processedPoints = new Queue<PreProcessedPoints>();

                foreach (XmlNode point in points!)
                {
                    var code = point.Attributes!["name"]!.Value;
                    ICodePoint? codePoint = codepointService.GetCodepoint(code);
                    
                    if (codePoint == null)
                        throw new XmlSchemaException("Waypoint " + code +
                                                     " could not be found in aerodrome, waypoint, or navaids data");
                    
                    var trackNode = point.SelectSingleNode("track");
                    var lsaltNode = point.SelectSingleNode("lsalt");

                    var hasDist = double.TryParse(point.Attributes!["dist"]?.Value, out var dist);
                    var hasTrackIn = int.TryParse(trackNode!.Attributes!["in"]?.Value, out var trackIn);
                    var hasTrackOut = int.TryParse(trackNode.Attributes!["out"]?.Value, out var trackOut);
                    var hasLsaltIn = int.TryParse(lsaltNode?.Attributes!["in"]?.Value, out var lsaltIn);
                    var hasLsaltOut = int.TryParse(lsaltNode?.Attributes!["out"]?.Value, out var lsaltOut);
                    
                    processedPoints.Enqueue(
                        new PreProcessedPoints(
                            codePoint,
                            hasDist ? dist : null,
                            point.Attributes!["level"]?.Value,
                            hasTrackIn ? trackIn : null,
                            hasTrackOut ? trackOut : null,
                            hasLsaltIn ? lsaltIn : null,
                            hasLsaltOut ? lsaltOut : null
                        ));
                }
                
                var limitedSegment = node.Attributes!["limited-segment"] != null &&
                                     node.Attributes!["limited-segment"]!.Value.ToLower() == "true";
                _airways.Add(node.Attributes!["name"]!.Value.ToUpper(),
                    new Airway(
                        node.Attributes!["name"]!.Value,
                        node.Attributes!["two-way"]!.Value.ToLower() == "true",
                        limitedSegment,
                        processedPoints
                    ));
            }
        }
        catch (Exception)
        {
            logger.LogWarning("Given airways XML file is invalid or malformed");
        }
    }

    public Airway? GetAirway(string code)
    {
        if(_airways.TryGetValue(code.ToUpper(), out var airway))
            return airway;
        return null;
    }
}