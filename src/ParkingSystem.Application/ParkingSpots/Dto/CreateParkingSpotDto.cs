

using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.ParkingSpots.Dto;

public class CreateParkingSpotDto
{

    [Required]
    [StringLength(30)]
    public string SpotCode { get; set; }

    [GreaterThanZero]
    public long ParkingAreaId { get; set; }
}
