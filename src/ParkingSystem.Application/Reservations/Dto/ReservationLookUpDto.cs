
using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System.Collections.Generic;

namespace ParkingSystem.Reservations.Dto;

public class ReservationCustomerLookUpDto:EntityDto<long>
{
    public string Name { get; set; }
    public string PhoneNumber { get; set; }

    public string DisplayText => string.IsNullOrWhiteSpace(PhoneNumber)
        ? Name
        : $"{Name} ({PhoneNumber})";
}

public class ReservationParkingAreaLookUpDto: EntityDto<long>
{
    public string ParkingCode { get; set; }
    public VehicleType VehicleType { get; set; }

    public string DisplayText => $"{ParkingCode} - {VehicleType}";
}
public class ReservationParkingSpotLookUpDto: EntityDto<long>
{
    public string SpotCode { get; set; }

    public string DisplayText => $"{SpotCode}";
}

public class ReservationLookUpDto
{
    public List<ReservationCustomerLookUpDto> Customers { get; set; }
    public List<ReservationParkingAreaLookUpDto> ParkingAreas { get; set; }
    public List<ReservationParkingSpotLookUpDto> ParkingSpots{ get; set; }
}