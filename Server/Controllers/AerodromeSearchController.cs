using ATCTools.Server.Models;
using ATCTools.Server.Services;
using Microsoft.AspNetCore.Mvc;
using ATCTools.Shared;
using ATCTools.Shared.Models;

namespace ATCTools.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AerodromeSearchController
{
    private readonly ILogger<AerodromeSearchController> _logger;
    private readonly AerodromeService _aerodromeService;
    
    public AerodromeSearchController(
        ILogger<AerodromeSearchController> logger,
        AerodromeService aerodromeService)
    {
        _logger = logger;
        _aerodromeService = aerodromeService;
    }

    [HttpGet]
    public IEnumerable<ClientAerodrome> SearchAerodromes([FromQuery] string? search)
    {
        var aerodromes = 
            string.IsNullOrWhiteSpace(search) ? 
                _aerodromeService.GetAerodromesWithSoC().OrderBy(a => a.Name) : 
                _aerodromeService.Search(search).OrderBy(a => a.Name).Take(10);

        return aerodromes.Select(a => new ClientAerodrome
        {
            Name = a.Name,
            Code = a.Code,
            State = a.State,
            Location = a.Location,
            SoP = a.SoP,
            Parent = a.Parent,
            Charts = a.Charts.Select(c => new ClientAerodromeChart
            {
                Name = c.Name,
                Path = c.Path,
                Updated = c.Updated,
                Amendment = c.Amendment
            }).ToArray(),
            Sids = a.Sids.Select(s => new ClientSid
            {
                Name = s.Name,
                AircraftType = s.AircraftType,
                Runways = s.Runways,
                IsRadar = s.Radar,
                Transitions = string.Join(' ',
                    s.Transitions?.Select(t =>
                        t.Type == TransitionType.NAV ? t.Code : "RADAR" + (t.Track.HasValue ? "-" + t.Track : "")) ?? Array.Empty<string?>())
            }).ToArray(),
            Stars = a.Stars.Select(s => new ClientStar
            {
                Name = s.Name,
                AircraftType = s.AircraftType,
                Runways = s.Runways,
                Transitions = string.Join(' ',
                    s.Transitions.Select(t =>
                        t.Code + (t.Runways != null ? " (RWY " + t.Runways +")" : "") + 
                        (t.AircraftType != AircraftType.BOTH ? t.AircraftType == AircraftType.JET ? " (JET)" : " (NON-JET)" : "")))
            }).ToArray()
        }).ToArray();
    }

    [HttpGet("{code}")]
    public ClientAerodrome? GetAerodrome(string code)
    {
        var aerodrome = _aerodromeService.GetAerodrome(code);
        
        if (aerodrome == null)
            return null;
        
        return new ClientAerodrome
        {
            Name = aerodrome.Name,
            Code = aerodrome.Code,
            State = aerodrome.State,
            Location = aerodrome.Location,
            SoP = aerodrome.SoP,
            Parent = aerodrome.Parent,
            Charts = aerodrome.Charts.Select(c => new ClientAerodromeChart
            {
                Name = c.Name,
                Path = c.Path,
                Updated = c.Updated,
                Amendment = c.Amendment
            }).ToArray(),
            Sids = aerodrome.Sids.Select(s =>
            new ClientSid
            {
                Name = s.Name,
                AircraftType = s.AircraftType,
                Runways = s.Runways,
                IsRadar = s.Radar,
                Transitions = string.Join(' ',
                    s.Transitions?.Select(t =>
                        t.Type == TransitionType.NAV ? t.Code : "RADAR" + (t.Track.HasValue ? "-" + t.Track : "")) ?? Array.Empty<string?>())
            }).ToArray(),
            Stars = aerodrome.Stars.Select(s => new ClientStar
            {
                Name = s.Name,
                AircraftType = s.AircraftType,
                Runways = s.Runways,
                Transitions = string.Join(' ',
                    s.Transitions.Select(t =>
                        t.Code + (t.Runways != null ? " (RWY " + t.Runways +")" : "") + 
                        (t.AircraftType != AircraftType.BOTH ? t.AircraftType == AircraftType.JET ? " (JET)" : " (NON-JET)" : "")))
            }).ToArray()
        };
    }
}