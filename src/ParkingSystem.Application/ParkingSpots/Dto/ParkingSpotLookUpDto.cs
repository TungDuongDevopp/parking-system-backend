

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;

namespace ParkingSystem.ParkingSpots.Dto;

public class ParkingSpotLookUpDto:EntityDto<long>
{
    public string ParkingCode { get; set; }
    public VehicleType VehicleType { get; set; }
    public string DisplayText => $"{ParkingCode} - {VehicleType}";
}
