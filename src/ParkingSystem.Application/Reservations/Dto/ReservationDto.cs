
using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System;

namespace ParkingSystem.Reservations.Dto;

public class ReservationDto: EntityDto<long>
{
    public long CustomerId { get; set; }
    public string CustomerName { get; set; }
    public long ParkingAreaId { get; set; }
    public string ParkingAreaCode { get; set; }
    public string ParkingAreaName { get; set; }
    public long? ParkingSpotId { get; set; }
    public string? ParkingSpotCode { get; set; }
    public DateTime ReservedAt { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime EndTime { get; set; }
    public VehicleType? VehicleType { get; set; }
    public string? VehicleTypeName => VehicleType?.ToString();
}
