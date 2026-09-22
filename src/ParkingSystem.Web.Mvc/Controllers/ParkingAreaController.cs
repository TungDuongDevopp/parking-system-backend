using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Authorization;
using ParkingSystem.Controllers;
using ParkingSystem.ParkingAreas;
using ParkingSystem.Web.Models.ParkingArea;
using ParkingSystem.Web.Models.Vehicle;
using System.Threading.Tasks;

namespace ParkingSystem.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_ParkingAreas)]
public class ParkingAreaController : ParkingSystemControllerBase
    
{
    private readonly IParkingAreaAppService _parkingAreaAppService;

    public ParkingAreaController(IParkingAreaAppService parkingAreaAppService)
    {
        _parkingAreaAppService = parkingAreaAppService;
    }
    public IActionResult Index()
    {
        return View();
    }
    public async Task<ActionResult> EditModal(long parkingAreaId)
    {
        var parkingArea = await _parkingAreaAppService.GetAsync(new EntityDto<long>(parkingAreaId));
        var model = new EditParkingAreaViewModel
        {
            ParkingArea = parkingArea
        };

        return PartialView("_EditModal", model);
    }
}
