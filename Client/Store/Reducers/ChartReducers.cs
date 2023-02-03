using ATCTools.Client.Models;
using Fluxor;

namespace ATCTools.Client.Store.Reducers;

public class ChartReducers
{
    [ReducerMethod]
    public static ChartState ReduceSetAerodromeAction(ChartState chartState, SetAerodromeAction action) =>
        new (action.SelectedAerodrome, chartState.SelectedChart);
    
    [ReducerMethod]
    public static ChartState ReduceSetChartAction(ChartState chartState, SetChartAction action) =>
        new (chartState.SelectedAerodrome, action.SelectedChart);
}