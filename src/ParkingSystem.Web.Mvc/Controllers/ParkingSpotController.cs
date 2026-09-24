using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Authorization;
using ParkingSystem.Controllers;
using ParkingSystem.ParkingSpots;
using ParkingSystem.Web.Models.ParkingSpot;
using System.Threading.Tasks;


namespace ParkingSystem.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_ParkingSpots)]
public class ParkingSpotController : ParkingSystemControllerBase
{
    private readonly IParkingSpotAppService _parkingSpotAppService;
    public ParkingSpotController(IParkingSpotAppService parkingSpotAppService)
    {
        _parkingSpotAppService = parkingSpotAppService;
    }
    public IActionResult Index()
    {
        return View();
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
