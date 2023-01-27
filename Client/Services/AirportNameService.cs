using System.Net.Http.Json;
using ATCTools.Client.Models;

namespace ATCTools.Client.Services;

public class AirportNameService
{
    private readonly HttpClient _httpClient;
    
    private Dictionary<string, AirportInformation[]>? _airports = null;

    public AirportNameService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task LoadData()
    {
        _airports ??= await _httpClient.GetFromJsonAsync<Dictionary<string, AirportInformation[]>>("data/airport.json");
    }

    public AirportInformation? GetAirport(string code)
    {
        if (!string.IsNullOrWhiteSpace(code) && _airports != null && _airports.ContainsKey(code.ToUpper()))
            return _airports[code.ToUpper()].FirstOrDefault();
        return null;
    }
}