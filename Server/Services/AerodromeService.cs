using System.Xml;
using ATCTools.Server.Helpers;
using ATCTools.Server.Models;
using ATCTools.Shared.Models;

namespace ATCTools.Server.Services;

public class AerodromeService
{
    private readonly Dictionary<string, Aerodrome> _aerodromes = new();

    private readonly string SCHEMA_PATH = "XmlSchemas" + Path.DirectorySeparatorChar + "Aerodromes.xsd";
    private readonly string CHART_SCHEMA_PATH = "XmlSchemas" + Path.DirectorySeparatorChar + "AerodromeCharts.xsd";
    private readonly string SID_SCHEMA_PATH = "XmlSchemas" + Path.DirectorySeparatorChar + "AerodromeSIDs.xsd";
    private readonly string STAR_SCHEMA_PATH = "XmlSchemas" + Path.DirectorySeparatorChar + "AerodromeSTARs.xsd";

    public AerodromeService(ILogger<AerodromeService> logger, IConfiguration configuration)
    {
        using var schemaReader = XmlReader.Create(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + SCHEMA_PATH);
        using var dataReader = XmlReader.Create(configuration["DataFiles:Aerodromes"].Replace("~", Directory.GetCurrentDirectory()));
        
        XmlDocument data = new();
        data.Schemas.Add(null, schemaReader);
        data.Load(dataReader);

        try
        {
            data.Validate(null);
                
            var nodes = data.DocumentElement?.SelectNodes("aerodrome");

            if (nodes == null)
            {
                logger.LogWarning("No aerodromes present in the given XML file");
                return;
            }

            foreach (XmlNode node in nodes)
            {
                var locNode = node.SelectSingleNode("location");
                _aerodromes.Add(node.Attributes!["code"]!.Value.ToUpper(),
                    new Aerodrome(
                        node.Attributes!["code"]!.Value,
                        node.Attributes!["name"]!.Value,
                        ModelUtilities.GetState(node.Attributes!["state"]!.Value),
                        new Location
                        {
                            Latitude = ModelUtilities.GetCoordinate(locNode!.Attributes!["lat"]!.Value) ?? 0,
                            Longitude = ModelUtilities.GetCoordinate(locNode.Attributes!["long"]!.Value) ?? 0
                        },
                        node.Attributes!["sop"]?.Value,
                        node.Attributes!["parent"]?.Value
                    ));
            }
        }
        catch (Exception)
        {
            logger.LogWarning("Given aerodrome XML file is invalid or malformed");
        }
        
        using var chartSchemaReader = XmlReader.Create(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + CHART_SCHEMA_PATH);
        using var chartDataReader = XmlReader.Create(configuration["DataFiles:AerodromeCharts"].Replace("~", Directory.GetCurrentDirectory()));
        
        XmlDocument chartData = new();
        chartData.Schemas.Add(null, chartSchemaReader);
        chartData.Load(chartDataReader);

        try
        {
            chartData.Validate(null);
                
            var nodes = chartData.DocumentElement?.SelectNodes("aerodrome");

            if (nodes == null)
            {
                logger.LogWarning("No charts present in the given XML file");
                return;
            }

            foreach (XmlNode node in nodes)
            {
                var aerodrome = GetAerodrome(node.Attributes!["code"]!.Value);
                if(aerodrome == null)
                    continue;
                
                var charts = node.SelectNodes("chart");
                if(charts == null)
                    continue;

                foreach (XmlNode chart in charts)
                {
                    aerodrome.Charts.Add(
                        new AerodromeChart(
                            aerodrome,
                            chart.Attributes!["name"]!.Value,
                            chart.Attributes!["path"]!.Value,
                            chart.Attributes!["updated"]!.Value,
                            int.Parse(chart.Attributes!["am"]!.Value)
                        ));
                }
            }
        }
        catch (Exception)
        {
            logger.LogWarning("Given chart XML file is invalid or malformed");
        }
        
        using var sidSchemaReader = XmlReader.Create(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + SID_SCHEMA_PATH);
        using var sidDataReader = XmlReader.Create(configuration["DataFiles:AerodromeSIDs"].Replace("~", Directory.GetCurrentDirectory()));
        
        XmlDocument sidData = new();
        sidData.Schemas.Add(null, sidSchemaReader);
        sidData.Load(sidDataReader);

        try
        {
            sidData.Validate(null);
                
            var nodes = sidData.DocumentElement?.SelectNodes("aerodrome");

            if (nodes == null)
            {
                logger.LogWarning("No SIDs present in the given XML file");
                return;
            }

            foreach (XmlNode node in nodes)
            {
                var aerodrome = GetAerodrome(node.Attributes!["code"]!.Value);
                if(aerodrome == null)
                    continue;
                
                var sids = node.SelectNodes("sid");
                if(sids == null)
                    continue;

                foreach (XmlNode sid in sids)
                {
                    var departure = sid.SelectSingleNode("departure");
                    var aerodromeSid = new AerodromeSid
                    {
                        Name = sid.Attributes!["name"]!.Value,
                        Code = sid.Attributes!["code"]!.Value,
                        Radar = sid.Attributes!["radar"]?.Value == "true",
                        Runways = sid.Attributes!["runway"]!.Value,
                        AircraftType = ModelUtilities.GetAircraftType(sid.Attributes!["aircraft-type"]!.Value),
                        DepartureType = departure != null ? DepartureType.NAV : DepartureType.RADAR,
                        DepartureCode = departure?.Attributes!["code"]!.Value
                    };
                    
                    aerodrome.Sids.Add(aerodromeSid);
                    
                    var transitions = sid.SelectNodes("transition");

                    if (transitions == null)
                        continue;
                    
                    foreach (XmlNode transition in transitions)
                    {
                        var type = ModelUtilities.GetTransitionType(transition.Attributes!["type"]!.Value);
                        aerodromeSid.Transitions.Add(new SidTransition
                        {
                            Type = type,
                            Code = type == TransitionType.NAV ? transition.Attributes!["code"]!.Value : null,
                            Track = type == TransitionType.RADAR && transition.Attributes!["track"] != null ? 
                                            int.Parse(transition.Attributes!["track"]!.Value) : null
                        });
                    }
                }
            }
        }
        catch (Exception)
        {
            logger.LogWarning("Given SID XML file is invalid or malformed");
        }
        
        using var starSchemaReader = XmlReader.Create(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + STAR_SCHEMA_PATH);
        using var starDataReader = XmlReader.Create(configuration["DataFiles:AerodromeSTARs"].Replace("~", Directory.GetCurrentDirectory()));
        
        XmlDocument starData = new();
        starData.Schemas.Add(null, starSchemaReader);
        starData.Load(starDataReader);

        try
        {
            starData.Validate(null);
                
            var nodes = starData.DocumentElement?.SelectNodes("aerodrome");

            if (nodes == null)
            {
                logger.LogWarning("No STARs present in the given XML file");
                return;
            }

            foreach (XmlNode node in nodes)
            {
                var aerodrome = GetAerodrome(node.Attributes!["code"]!.Value);
                if(aerodrome == null)
                    continue;
                
                var stars = node.SelectNodes("star");
                if(stars == null)
                    continue;

                foreach (XmlNode star in stars)
                {
                    var aerodromeStar = new AerodromeStar
                    {
                        Name = star.Attributes!["name"]!.Value,
                        Code = star.Attributes!["code"]!.Value,
                        Runways = star.Attributes!["runway"]!.Value,
                        AircraftType = ModelUtilities.GetAircraftType(star.Attributes!["aircraft-type"]!.Value),
                        ArrivalCode = star.Attributes!["waypoint"]!.Value
                    };
                    
                    aerodrome.Stars.Add(aerodromeStar);
                    
                    var transitions = star.SelectNodes("transition");

                    if (transitions == null)
                        continue;
                    
                    foreach (XmlNode transition in transitions)
                    {
                        aerodromeStar.Transitions.Add(new StarTransition
                        {
                            Code = star.Attributes!["code"]!.Value,
                            Runways = star.Attributes!["runway"]?.Value,
                            AircraftType = ModelUtilities.GetAircraftType(star.Attributes!["aircraft-type"]?.Value)
                        });
                    }
                }
            }
        }
        catch (Exception)
        {
            logger.LogWarning("Given STAR XML file is invalid or malformed");
        }
    }

    public Aerodrome? GetAerodrome(string code)
    {
        if(_aerodromes.TryGetValue(code.ToUpper(), out var aerodrome))
            return aerodrome;
        return null;
    }
    
    public IEnumerable<Aerodrome> GetAerodromesWithSoC()
    {
        return _aerodromes.Values.Where(a => !string.IsNullOrWhiteSpace(a.SoP)).ToArray();
    }

    public IEnumerable<Aerodrome> Search(string search)
    {
        return _aerodromes.Values.Where(a => 
            a.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase) || 
            a.Code.Contains(search, StringComparison.InvariantCultureIgnoreCase)
            ).ToArray();
    }
    
    public IEnumerable<Aerodrome> GetAerodromesNear(Location location)
    {
        var x1 = location.Longitude - 0.05;
        var x2 = location.Longitude + 0.05;
        var y1 = location.Latitude - 0.05;
        var y2 = location.Latitude + 0.05;

        return _aerodromes.Values.Where(n =>
            x1 <= n.Location.Longitude &&
            x2 >= n.Location.Longitude &&
            y1 <= n.Location.Latitude &&
            y2 >= n.Location.Latitude);
    }

    public void InjectSidStartPoints(CodepointService codepointService)
    {
        var sids = _aerodromes.Values.SelectMany(a => a.Sids);
        var stars = _aerodromes.Values.SelectMany(a => a.Stars);

        foreach (var sid in sids)
        {
            if(sid.DepartureCode != null)
                sid.Point = codepointService.GetCodepoint(sid.DepartureCode);
            foreach (var transition in sid.Transitions.Where(t => t.Code != null))
                transition.Point = codepointService.GetCodepoint(transition.Code!);
        }

        foreach (var star in stars)
        {
            star.Point = codepointService.GetCodepoint(star.ArrivalCode)!;
            foreach (var transition in star.Transitions)
                transition.Point = codepointService.GetCodepoint(transition.Code)!;
        }
    }
}