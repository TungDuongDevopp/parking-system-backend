using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Authorization;
using ParkingSystem.Controllers;
using ParkingSystem.Customers;
using ParkingSystem.Customers.Dto;
using ParkingSystem.Entities.Enums;
using ParkingSystem.ParkingAreas;
using ParkingSystem.ParkingAreas.Dto;
using ParkingSystem.ParkingSpots;
using ParkingSystem.ParkingSpots.Dto;
using ParkingSystem.Quotations;
using ParkingSystem.Quotations.Dto;
using ParkingSystem.Reservations;
using ParkingSystem.Reservations.Dto;
using ParkingSystem.Staffs;
using ParkingSystem.Staffs.Dto;
using ParkingSystem.Subscriptions;
using ParkingSystem.Subscriptions.Dto;
using ParkingSystem.Vehicles;
using ParkingSystem.Vehicles.Dto;
using ParkingSystem.Web.Models.Home;

namespace ParkingSystem.Web.Controllers;

[AbpMvcAuthorize]
public class HomeController : ParkingSystemControllerBase
{
    private readonly IParkingAreaAppService _parkingAreaAppService;
    private readonly IParkingSpotAppService _parkingSpotAppService;
    private readonly IReservationAppService _reservationAppService;
    private readonly ICustomerAppService _customerAppService;
    private readonly IVehicleAppService _vehicleAppService;
    private readonly ISubscriptionAppService _subscriptionAppService;
    private readonly IQuotationAppService _quotationAppService;
    private readonly IStaffAppService _staffAppService;

    public HomeController(
        IParkingAreaAppService parkingAreaAppService,
        IParkingSpotAppService parkingSpotAppService,
        IReservationAppService reservationAppService,
        ICustomerAppService customerAppService,
        IVehicleAppService vehicleAppService,
        ISubscriptionAppService subscriptionAppService,
        IQuotationAppService quotationAppService,
        IStaffAppService staffAppService)
    {
        _parkingAreaAppService = parkingAreaAppService;
        _parkingSpotAppService = parkingSpotAppService;
        _reservationAppService = reservationAppService;
        _customerAppService = customerAppService;
        _vehicleAppService = vehicleAppService;
        _subscriptionAppService = subscriptionAppService;
        _quotationAppService = quotationAppService;
        _staffAppService = staffAppService;
    }

    public async Task<ActionResult> Index(string role = null)
    {
        var model = new HomeViewModel();

        // 1. Role determination
        bool isAdmin = await IsGrantedAsync(PermissionNames.Pages_Users) ||
                       await IsGrantedAsync(PermissionNames.Pages_Roles) ||
                       await IsGrantedAsync(PermissionNames.Pages_Tenants);

        bool isStaff = !isAdmin && (
                       await IsGrantedAsync(PermissionNames.Pages_Staffs) ||
                       await IsGrantedAsync(PermissionNames.Pages_Customers_ViewAll) ||
                       await IsGrantedAsync(PermissionNames.Pages_ParkingSpots_Manager) ||
                       await IsGrantedAsync(PermissionNames.Pages_Reservations_ViewAll));

        var effectiveRole = isAdmin ? UserHomeRole.Admin : (isStaff ? UserHomeRole.Staff : UserHomeRole.Customer);

        // Allow Admin to preview other roles via ?role=customer or ?role=staff
        if (isAdmin && !string.IsNullOrWhiteSpace(role))
        {
            if (Enum.TryParse<UserHomeRole>(role, true, out var previewRole))
            {
                effectiveRole = previewRole;
            }
        }

        model.Role = effectiveRole;
        model.CanSwitchRoleView = isAdmin;

        model.DisplayUserName = User.Identity?.Name ?? "User";

        // 2. Populate data based on role
        if (effectiveRole == UserHomeRole.Customer)
        {
            await PopulateCustomerData(model);
        }
        else if (effectiveRole == UserHomeRole.Staff)
        {
            await PopulateStaffData(model);
        }
        else
        {
            await PopulateAdminData(model);
        }

        return View(model);
    }

    private async Task PopulateCustomerData(HomeViewModel model)
    {
        try
        {
            var customer = await _customerAppService.GetMyProfileAsync();
            if (customer != null)
            {
                model.CustomerInfo.HasProfile = true;
                model.CustomerInfo.CustomerName = customer.Name;
                model.CustomerInfo.PhoneNumber = customer.PhoneNumber;
                model.CustomerInfo.Email = customer.Email;
            }
            else
            {
                model.CustomerInfo.HasProfile = false;
                model.CustomerInfo.CustomerName = model.DisplayUserName;
            }
        }
        catch
        {
            model.CustomerInfo.HasProfile = false;
            model.CustomerInfo.CustomerName = model.DisplayUserName;
        }
    }

    private async Task PopulateStaffData(HomeViewModel model)
    {
        try
        {
            // Parking Areas
            var areasResult = await _parkingAreaAppService.GetAllAsync(new PagedParkingAreaResultRequestDto { MaxResultCount = 100 });
            model.StaffStats.ParkingAreas = areasResult.Items.ToList();

            // Parking Spots
            var spotsResult = await _parkingSpotAppService.GetAllAsync(new PagedParkingSpotResultRequestDto { MaxResultCount = 500 });
            var spots = spotsResult.Items.ToList();
            model.StaffStats.Spots = spots;
            model.StaffStats.TotalSpots = spotsResult.TotalCount;
            model.StaffStats.AvailableSpots = spots.Count(s => s.Status == ParkingSpotStatus.Available);
            model.StaffStats.OccupiedSpots = spots.Count(s => s.Status == ParkingSpotStatus.Occupied);
            model.StaffStats.ReservedSpots = spots.Count(s => s.Status == ParkingSpotStatus.Reserved);
            model.StaffStats.UnavailableSpots = spots.Count(s => s.Status == ParkingSpotStatus.Unavailable);

            // Active / Today Reservations
            var resResult = await _reservationAppService.GetAllAsync(new PagedReservationResultRequestDto { MaxResultCount = 30 });
            model.StaffStats.TodayReservations = resResult.Items
                .Where(r => r.Status == ReservationStatus.Reserved)
                .OrderByDescending(r => r.ReservedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.Warn("Error loading staff home stats: " + ex.Message);
        }
    }

    private async Task PopulateAdminData(HomeViewModel model)
    {
        try
        {
            // 1. Parking Areas
            var areasResult = await _parkingAreaAppService.GetAllAsync(new PagedParkingAreaResultRequestDto { MaxResultCount = 100 });
            model.AdminStats.ParkingAreas = areasResult.Items.ToList();
            model.AdminStats.TotalParkingAreas = areasResult.TotalCount;
            model.AdminStats.ActiveParkingAreas = areasResult.Items.Count(a => a.Status == ParkingAreaStatus.Active);

            // 2. Parking Spots
            var spotsResult = await _parkingSpotAppService.GetAllAsync(new PagedParkingSpotResultRequestDto { MaxResultCount = 1000 });
            var spots = spotsResult.Items.ToList();
            model.AdminStats.TotalParkingSpots = spotsResult.TotalCount;
            model.AdminStats.AvailableSpots = spots.Count(s => s.Status == ParkingSpotStatus.Available);
            model.AdminStats.OccupiedSpots = spots.Count(s => s.Status == ParkingSpotStatus.Occupied);
            model.AdminStats.ReservedSpots = spots.Count(s => s.Status == ParkingSpotStatus.Reserved);
            model.AdminStats.UnavailableSpots = spots.Count(s => s.Status == ParkingSpotStatus.Unavailable);

            // 3. Customers
            var custResult = await _customerAppService.GetAllAsync(new PagedCustomerResultRequestDto { MaxResultCount = 1 });
            model.AdminStats.TotalCustomers = custResult.TotalCount;

            // 4. Reservations
            var resResult = await _reservationAppService.GetAllAsync(new PagedReservationResultRequestDto { MaxResultCount = 20 });
            model.AdminStats.TotalReservations = resResult.TotalCount;
            model.AdminStats.RecentReservations = resResult.Items.ToList();
            model.AdminStats.ActiveReservations = resResult.Items.Count(r => r.Status == ReservationStatus.Reserved);
            model.AdminStats.CompletedReservations = resResult.Items.Count(r => r.Status == ReservationStatus.Completed);

            // 5. Subscriptions
            var subResult = await _subscriptionAppService.GetAllAsync(new PagedSubscriptionResultRequestDto { MaxResultCount = 1 });
            model.AdminStats.TotalSubscriptions = subResult.TotalCount;

            // 6. Quotations
            var quotResult = await _quotationAppService.GetAllAsync(new PagedQuotationResultRequestDto { MaxResultCount = 1 });
            model.AdminStats.TotalQuotations = quotResult.TotalCount;

            // 7. Vehicles
            var vehResult = await _vehicleAppService.GetAllAsync(new PagedVehicleResultRequestDto { MaxResultCount = 1 });
            model.AdminStats.TotalVehicles = vehResult.TotalCount;

            // 8. Staffs
            var staffResult = await _staffAppService.GetAllAsync(new PagedStaffResultRequestDto { MaxResultCount = 1 });
            model.AdminStats.TotalStaffs = staffResult.TotalCount;
        }
        catch (Exception ex)
        {
            Logger.Warn("Error loading admin dashboard stats: " + ex.Message);
        }
    }
}
