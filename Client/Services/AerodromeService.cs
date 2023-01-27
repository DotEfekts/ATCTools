using System.Net.Http.Json;
using ATCTools.Shared.Models;
using Blazored.LocalStorage;

namespace ATCTools.Client.Services;

public class AerodromeService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    
    public ClientAerodrome? SelectedAerodrome { get; private set; } = null;
    public IEnumerable<ClientAerodrome>? DefaultAerodromes { get; private set; } = null;

    public AerodromeService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task LoadDefaultData()
    {
        if (await _localStorage.ContainKeyAsync("selectedAerodrome"))
            SetValue(await _httpClient.GetFromJsonAsync<ClientAerodrome>($"AerodromeSearch/" + await _localStorage.GetItemAsync<string>("selectedAerodrome")));
        DefaultAerodromes = await _httpClient.GetFromJsonAsync<IEnumerable<ClientAerodrome>>($"AerodromeSearch");
    }

    public event Action<ClientAerodrome?>? OnStateChange;
    
    public void SetValue(ClientAerodrome? value)
    { 
        SelectedAerodrome = value;
        NotifySelectionChanged();
    }
    
    private void NotifySelectionChanged() => OnStateChange?.Invoke(SelectedAerodrome);
}