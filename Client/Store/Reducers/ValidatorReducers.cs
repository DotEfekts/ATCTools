using ATCTools.Client.Models;
using Fluxor;

namespace ATCTools.Client.Store.Reducers;

public class ValidatorReduces
{
    [ReducerMethod]
    public static GeneratedRouteState ReduceSetDepartingAerodromeAction(GeneratedRouteState routeState, SetDepartingAerodromeAction action) =>
        new (routeState.Route, action.Aerodrome);
    
    [ReducerMethod]
    public static GeneratedRouteState ReduceSetRouteAction(GeneratedRouteState routeState, SetRouteAction action) =>
        new (action.Route, routeState.DepartingAerodrome);
    
    [ReducerMethod]
    public static FieldAirportState ReduceSetDepartingFieldAction(FieldAirportState fieldState, SetDepartingFieldAction action) =>
        new (action.Information, fieldState.DestinationFieldInfo);
    
    [ReducerMethod]
    public static FieldAirportState ReduceSetDestinationFieldAction(FieldAirportState fieldState, SetDestinationFieldAction action) =>
        new (fieldState.DepartingFieldInfo, action.Information);
    
    [ReducerMethod]
    public static MetarState ReduceSetMetarAction(MetarState metarState, SetMetarAction action) =>
        new (action.Metar);
    
    [ReducerMethod]
    public static ClientValidationState ReduceSetValidationResultAction(ClientValidationState validationState, SetValidationResultAction action) =>
        new (action.Result, action.DepartingInfo, action.DestinationInfo);
}