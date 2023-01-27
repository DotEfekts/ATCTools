using System.ComponentModel.DataAnnotations;

namespace ATCTools.Shared.Models;

public class FlightPlan
{
    [Required]
    public string Route { get; set; }
    
    [Required]
    [Display(Name = "Departing Airport")]
    public string DepartingAirport { get; set; }
    
    [Required]
    [Display(Name = "Destination Airport")]
    public string DestinationAirport { get; set; }
}