

using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Vehicles.Dto;
public class CreateVehicleDto
{
  
    [Required]
    [EnumDataType(typeof(VehicleType))]
    public VehicleType VehicleType { get; set; }

    [StringLength(30)]
    public string? LicensePlate { get; set; }

    [StringLength(255)]
    public string? Brand { get; set; }

    [StringLength(255)]
    [Required]
    public string Color { get; set; }

    [GreaterThanZero]
    public long? CustomerId { get; set; }
}
