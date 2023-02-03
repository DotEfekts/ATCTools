using ATCTools.Client.Models;
using ATCTools.Shared.Models;
using Microsoft.VisualBasic;

namespace ATCTools.Client.Store;

public class SetDepartingFieldAction
{
    public AirportInformation? Information { get; }
    public SetDepartingFieldAction(AirportInformation? information)
    {
        Information = information;
    }
}

public class SetDestinationFieldAction
{
    public AirportInformation? Information { get; }
    public SetDestinationFieldAction(AirportInformation? information)
    {
        Information = information;
    }
}

public class SetDepartingAerodromeAction
{
    public ClientAerodrome? Aerodrome { get; }
    public SetDepartingAerodromeAction(ClientAerodrome? aerodrome)
    {
        Aerodrome = aerodrome;
    }
}

public class SetMetarAction
{
    public WeatherData? Metar { get; }
    public SetMetarAction(WeatherData? metar)
    {
        Metar = metar;
    }
}

public class SetValidationResultAction
{
    public PlanValidationResult? Result { get; set; }
    public AirportInformation? DepartingInfo { get; }
    public AirportInformation? DestinationInfo { get; }
    
    public SetValidationResultAction(PlanValidationResult? result, AirportInformation? departingInfo, AirportInformation? destinationInfo)
    {
        Result = result;
        DepartingInfo = departingInfo;
        DestinationInfo = destinationInfo;
    }
}

public class SetRouteAction
{
    public GeneratedRoute? Route { get; }
    public SetRouteAction(GeneratedRoute? route)
    {
        Route = route;
    }
}