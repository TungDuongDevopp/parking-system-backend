using ParkingSystem.Entities.Enums;
using System.Collections.Generic;

namespace ParkingSystem.Web.Models.Reservation;

public class ReservationCustomerLookUpDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string PhoneNumber { get; set; }

    public string DisplayText => string.IsNullOrWhiteSpace(PhoneNumber)
        ? Name
        : $"{Name} ({PhoneNumber})";
}

public class ReservationParkingAreaLookUpDto
{
    public long Id { get; set; }
    public string ParkingCode { get; set; }

    public VehicleType VehicleType { get; set; }

    public string DisplayText => $"{ParkingCode} - {VehicleType}";
}

public class ReservationParkingSpotLookUpDto
{
    public long Id { get; set; }

    public string SpotCode { get; set; }

    public string DisplayText => $"{SpotCode}";
}
public class ReservationListViewModel
{
    public IReadOnlyList<ReservationCustomerLookUpDto> Customers = new List<ReservationCustomerLookUpDto>();
    public IReadOnlyList<ReservationParkingAreaLookUpDto> ParkingAreas = new List<ReservationParkingAreaLookUpDto>();
    public IReadOnlyList<ReservationParkingSpotLookUpDto> ParkingSpots = new List<ReservationParkingSpotLookUpDto>();


}
