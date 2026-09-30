using ParkingSystem.Entities.Enums;
using System.Collections.Generic;

namespace ParkingSystem.Web.Models.ParkingSpot;

public class ParkingSpotParkingAreaLookUpDto
{
    public long Id { get; set; }
    public string ParkingCode { get; set; }

    public VehicleType VehicleType { get; set; }

    public string DisplayText => $"{ParkingCode} - {VehicleType}";
}
public class ParkingSpotListViewModel
{
    public IReadOnlyList<ParkingSpotParkingAreaLookUpDto> ParkingAreas = new List<ParkingSpotParkingAreaLookUpDto>();
}
