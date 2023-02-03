using ATCTools.Shared.Models;

namespace ATCTools.Client.Store;

public class SetAerodromeAction
{
    public ClientAerodrome? SelectedAerodrome { get; }

    public SetAerodromeAction(ClientAerodrome? selectedAerodrome)
    {
        SelectedAerodrome = selectedAerodrome;
    }
}

public class SetChartAction
{
    public ClientAerodromeChart? SelectedChart { get; }

    public SetChartAction(ClientAerodromeChart? selectedChart)
    {
        SelectedChart = selectedChart;
    }
}