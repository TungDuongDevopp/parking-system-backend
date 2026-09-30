using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Controllers;
using ParkingSystem.Entities;
using ParkingSystem.ParkingSpots;
using ParkingSystem.Web.Models.ParkingSpot;
using System.Linq;
using System.Threading.Tasks;


namespace ParkingSystem.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_ParkingSpots)]
public class ParkingSpotController : ParkingSystemControllerBase
{
    private readonly IParkingSpotAppService _parkingSpotAppService;
    private readonly IRepository<ParkingSpot, long> _parkingSpotRepository;
    private readonly IRepository<ParkingArea, long> _parkingAreaRepository;
    public ParkingSpotController(IParkingSpotAppService parkingSpotAppService,
        IRepository<ParkingSpot, long> parkingSpotRepository,
        IRepository<ParkingArea, long> parkingAreaRepository)
    {
        _parkingSpotAppService = parkingSpotAppService;
        _parkingAreaRepository = parkingAreaRepository;
        _parkingSpotRepository = parkingSpotRepository;
    }
    public async Task<IActionResult> Index()
    {
        var subQuery = _parkingSpotRepository.GetAll().AsNoTracking();
        
        var parkingAreaIds = subQuery.Select(s => s.ParkingAreaId).Distinct();
        
        var parkingAreas = await _parkingAreaRepository.GetAll()
          .AsNoTracking()
          .Where(a => parkingAreaIds.Contains(a.Id))
          .OrderBy(a => a.ParkingCode)
          .Select(a => new ParkingSpotParkingAreaLookUpDto
          {
              Id = a.Id,
              ParkingCode = a.ParkingCode,
              VehicleType = a.VehicleType
          })
          .ToListAsync();
        var model = new ParkingSpotListViewModel
        {
            ParkingAreas = parkingAreas
        };
        return View(model);
    }
    public async Task<ActionResult> EditModal(long parkingSpotId)
    {
        var parkingSpot = await _parkingSpotAppService.GetAsync(new EntityDto<long>(parkingSpotId));
        var model = new EditParkingSpotViewModel
        {
            ParkingSpot = parkingSpot
        };

        return PartialView("_EditModal", model);
    }
}
