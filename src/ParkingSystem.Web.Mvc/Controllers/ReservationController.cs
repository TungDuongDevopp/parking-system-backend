using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Authorization;
using ParkingSystem.Controllers;
using ParkingSystem.Reservations;
using ParkingSystem.Web.Models.Reservation;
using System.Threading.Tasks;

namespace ParkingSystem.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Reservations)]
public class ReservationController : ParkingSystemControllerBase
{
    private readonly IReservationAppService _reservationAppService;
  public ReservationController(IReservationAppService reservationAppService)
    {
        _reservationAppService = reservationAppService;
   
    }
    public async Task<IActionResult> Index()
    {
        var canViewAll = await IsGrantedAsync(PermissionNames.Pages_Reservations_ViewAll);
        if (!canViewAll)
        {
            return RedirectToAction(nameof(MyReservations));
        }

        var reservationInfo = await _reservationAppService.GetReservationLookUpAsync();

        var model = new ReservationListViewModel
        {
            Customers = reservationInfo.Customers,
            ParkingAreas = reservationInfo.ParkingAreas,
            ParkingSpots = reservationInfo.ParkingSpots
        };

        return View(model);
    }

    /// <summary>
    /// Customer-facing "My Reservations" page.
    /// Managers/admins who land here are redirected to the admin Index.
    /// </summary>
    public async Task<IActionResult> MyReservations()
    {
        if (await IsGrantedAsync(PermissionNames.Pages_Reservations_ViewAll))
        {
            return RedirectToAction(nameof(Index));
        }

        return View();
    }
}
