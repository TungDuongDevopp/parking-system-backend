
using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.ParkingSessions.Dto;
public class ParkingSessionCheckInDto
{
    [GreaterThanZero]
    public long? ParkingAreaId { get; set; }
   
    [GreaterThanZero]
    public long? ParkingSpotId { get; set; }
    public string? PlateNumber { get; set; }
    
    [GreaterThanZero]
    public long? ReservationId { get; set; }
    
    [EnumDataType (typeof(VehicleType))]
    [GreaterThanZero]
    public VehicleType VehicleType { get; set; }
}
