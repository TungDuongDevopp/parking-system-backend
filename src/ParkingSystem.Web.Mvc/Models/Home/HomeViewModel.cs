using System.Collections.Generic;
using ParkingSystem.ParkingAreas.Dto;
using ParkingSystem.ParkingSpots.Dto;
using ParkingSystem.Reservations.Dto;

namespace ParkingSystem.Web.Models.Home;

public enum UserHomeRole
{
    Admin,
    Staff,
    Customer
}

public class HomeViewModel
{
    public UserHomeRole Role { get; set; }
    public string DisplayUserName { get; set; }
    public bool CanSwitchRoleView { get; set; } // Only Admin can switch view

    public AdminDashboardDto AdminStats { get; set; } = new();
    public StaffDashboardDto StaffStats { get; set; } = new();
    public CustomerDashboardDto CustomerInfo { get; set; } = new();
}

public class AdminDashboardDto
{
    public int TotalParkingAreas { get; set; }
    public int ActiveParkingAreas { get; set; }
    public int TotalParkingSpots { get; set; }
    public int AvailableSpots { get; set; }
    public int OccupiedSpots { get; set; }
    public int ReservedSpots { get; set; }
    public int UnavailableSpots { get; set; }

    public int TotalCustomers { get; set; }
    public int TotalReservations { get; set; }
    public int ActiveReservations { get; set; }
    public int CompletedReservations { get; set; }
    public int TotalSubscriptions { get; set; }
    public int TotalQuotations { get; set; }
    public int TotalVehicles { get; set; }
    public int TotalStaffs { get; set; }

    public List<ParkingAreaDto> ParkingAreas { get; set; } = new();
    public List<ReservationDto> RecentReservations { get; set; } = new();
}

public class StaffDashboardDto
{
    public int TotalSpots { get; set; }
    public int AvailableSpots { get; set; }
    public int OccupiedSpots { get; set; }
    public int ReservedSpots { get; set; }
    public int UnavailableSpots { get; set; }

    public List<ParkingAreaDto> ParkingAreas { get; set; } = new();
    public List<ParkingSpotDto> Spots { get; set; } = new();
    public List<ReservationDto> TodayReservations { get; set; } = new();
}

public class CustomerDashboardDto
{
    public string CustomerName { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public bool HasProfile { get; set; }
}
