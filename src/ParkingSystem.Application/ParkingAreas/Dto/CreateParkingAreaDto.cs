
using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.ParkingAreas.Dto;

public class CreateParkingAreaDto
{

    [Required]
    [StringLength(30)]
    public string ParkingCode { get; set; }

    [Required]
    [StringLength(30)]
    public string Name { get; set; }

    [EnumDataType(typeof(VehicleType))]
    public VehicleType VehicleType { get; set; }

    [GreaterThanZero]
    public int Capacity { get; set; }

    [EnumDataType(typeof(ParkingMode))]
    public ParkingMode ParkingMode { get; set; }

    [Required]
    [StringLength(255)]
    public string Location { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }
}
