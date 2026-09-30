using Abp.AspNetCore.Mvc.Authorization;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Controllers;
using ParkingSystem.Entities;
using ParkingSystem.Reservations;
using ParkingSystem.Web.Models.Reservation;
using System.Linq;
using System.Threading.Tasks;

namespace ParkingSystem.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Reservations)]
public class ReservationController : ParkingSystemControllerBase
{
    private readonly IReservationAppService _reservationAppService;
    private readonly IRepository<Customer, long> _customerRepository;
    private readonly IRepository<ParkingArea, long> _parkingAreaRepository;
    private readonly IRepository<ParkingSpot, long> _parkingSpotRepository;
    private readonly IRepository<Reservation, long> _reservationRepository;



    public ReservationController(IReservationAppService reservationAppService,
          IRepository<Customer, long> customerRepository,
          IRepository<ParkingArea, long> parkingAreaRepository,
          IRepository<ParkingSpot, long> parkingSpotRepository,
          IRepository<Reservation, long> reservationRepository
        )
    {
        _reservationAppService = reservationAppService;
        _customerRepository = customerRepository;
        _parkingAreaRepository = parkingAreaRepository;
        _parkingSpotRepository = parkingSpotRepository;
        _reservationRepository = reservationRepository;
    }
    public async Task<IActionResult> Index()
    {
        var canViewAll = await IsGrantedAsync(PermissionNames.Pages_Reservations_ViewAll);
        if (!canViewAll)
        {
            return RedirectToAction(nameof(MyReservations));
        }

        var subQuery = _reservationRepository.GetAll().AsNoTracking();
        var customerIds = subQuery.Select(s => s.CustomerId).Distinct();
        var parkingAreaIds = subQuery.Select(s => s.ParkingAreaId).Distinct();
        var parkingSpotIds = subQuery.Select(s => s.ParkingSpotId).Distinct();

        var customers = await _customerRepository.GetAll()
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .Select(c => new ReservationCustomerLookUpDto
            {
                Id = c.Id,
                Name = c.Name,
                PhoneNumber = c.PhoneNumber
            })
            .ToListAsync();

        var parkingAreas = await _parkingAreaRepository.GetAll()
          .AsNoTracking()
          .Where(a => parkingAreaIds.Contains(a.Id))
          .OrderBy(a => a.ParkingCode)
          .Select(a => new ReservationParkingAreaLookUpDto
          {
              Id = a.Id,
              ParkingCode = a.ParkingCode,
              VehicleType = a.VehicleType
          })
          .ToListAsync();

        var parkingSpots = await _parkingSpotRepository.GetAll()
          .AsNoTracking()
          .Where(p => parkingAreaIds.Contains(p.Id))
          .OrderBy(p => p.SpotCode)
          .Select(p => new ReservationParkingSpotLookUpDto
          {
              Id = p.Id,
              SpotCode = p.SpotCode,
           
          })
          .ToListAsync();

        var model = new ReservationListViewModel
        {
            Customers = customers,
            ParkingAreas = parkingAreas,
            ParkingSpots = parkingSpots
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
