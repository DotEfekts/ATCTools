using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ATCTools.Client;
using ATCTools.Client.Services;
using Blazored.LocalStorage;
using Fluxor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMudServices();
builder.Services.AddScoped<AerodromeService>();
builder.Services.AddScoped<AirportNameService>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var currentAssembly = typeof(Program).Assembly;
builder.Services.AddFluxor(options => options.ScanAssemblies(currentAssembly));

var wasmHost = builder.Build();

await wasmHost.Services.GetService<AerodromeService>()?.LoadDefaultData()!;
await wasmHost.RunAsync();
