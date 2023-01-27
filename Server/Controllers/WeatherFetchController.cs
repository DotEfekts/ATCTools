using System.Web;
using ATCTools.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ATCTools.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherFetchController
{
    [HttpGet]
    public async Task<WeatherData> GetAerodromeData([FromQuery] string code)
    {
        var client = new HttpClient();
        var metarResult = await client.GetAsync($"https://metar.vatsim.net/?id={HttpUtility.UrlEncode(code)}");

        return new WeatherData
        {
            Metar = await metarResult.Content.ReadAsStringAsync(),
        };
    }
}