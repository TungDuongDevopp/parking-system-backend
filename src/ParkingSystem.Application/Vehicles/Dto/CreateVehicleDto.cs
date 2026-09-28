

using ParkingSystem.Entities.Enums;
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

    public long? CustomerId { get; set; }
}
