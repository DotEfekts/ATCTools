using ATCTools.Shared.Models;
using Fluxor;

namespace ATCTools.Client.Models;

[FeatureState]
public class FieldAirportState
{
    public AirportInformation? DepartingFieldInfo { get; }
    public AirportInformation? DestinationFieldInfo { get; }
    
    public FieldAirportState() {}

    public FieldAirportState(AirportInformation? departingField, AirportInformation? destinationField)
    {
        DepartingFieldInfo = departingField;
        DestinationFieldInfo = destinationField;
    }
}

[FeatureState]
public class ClientValidationState
{
    public PlanValidationResult? Result { get; set; }
    public AirportInformation? DepartingInfo { get; }
    public AirportInformation? DestinationInfo { get; }
    
    public ClientValidationState() {}

    public ClientValidationState(PlanValidationResult? result, AirportInformation? departing, AirportInformation? destination)
    {
        Result = result;
        DepartingInfo = departing;
        DestinationInfo = destination;
    }
}

[FeatureState]
public class MetarState
{
    public WeatherData? Metar { get; }
    
    public MetarState() {}

    public MetarState(WeatherData? metar)
    {
        Metar = metar;
    }

}

[FeatureState]
public class GeneratedRouteState
{
    public GeneratedRoute? Route { get; }
    public ClientAerodrome? DepartingAerodrome { get; }
    
    public GeneratedRouteState() {}

    public GeneratedRouteState(GeneratedRoute? route, ClientAerodrome? departing)
    {
        Route = route;
        DepartingAerodrome = departing;
    }
} 