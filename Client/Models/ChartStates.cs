using ATCTools.Shared.Models;
using Fluxor;

namespace ATCTools.Client.Models;

[FeatureState]
public class ChartState
{
    public ClientAerodrome? SelectedAerodrome { get; }
    public ClientAerodromeChart? SelectedChart { get; }
    
    public ChartState() {}

    public ChartState(ClientAerodrome? selectedAerodrome, ClientAerodromeChart? selectedChart)
    {
        SelectedAerodrome = selectedAerodrome;
        SelectedChart = selectedChart;
    }
}