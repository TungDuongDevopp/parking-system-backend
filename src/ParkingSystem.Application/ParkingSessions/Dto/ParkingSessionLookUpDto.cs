

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System.Collections.Generic;

namespace ParkingSystem.ParkingSessions.Dto;

public class SessionCustomerLookUpDto:EntityDto<long>
{
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public string DisplayText => string.IsNullOrWhiteSpace(PhoneNumber)
        ? Name
        : $"{Name} ({PhoneNumber})";
}
public class SessionAreaLookUpDto:EntityDto<long>
{
    public string ParkingCode { get; set; }
    public VehicleType VehicleType { get; set; }
    public string DisplayText => $"{ParkingCode} - {VehicleType}";
}
public class SessionSpotLookUpDto: EntityDto<long>
{
    public string SpotCode { get; set; }
    public string DisplayText => $"{SpotCode}";
}
public class SessionStaffLookUpDto: EntityDto<long>
{
    public long UserId { get; set; }
    public string Name { get; set; }
    public string DisplayText => $"{UserId} - {Name}";
}
public class ParkingSessionLookUpDto
{
   
    public List<SessionSpotLookUpDto> ParkingSpots { get; set; }
    public List<SessionAreaLookUpDto> ParkingAreas { get; set; }
    public List<SessionCustomerLookUpDto> Customers { get; set; }
    public List<SessionStaffLookUpDto> Staffs { get; set; }
}
