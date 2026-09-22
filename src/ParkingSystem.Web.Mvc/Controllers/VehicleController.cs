using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Authorization;
using ParkingSystem.Controllers;
using ParkingSystem.Vehicles;
using ParkingSystem.Web.Models.Vehicle;
using System.Threading.Tasks;

namespace ParkingSystem.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Vehicles)]
public class VehicleController : ParkingSystemControllerBase
{
    private readonly IVehicleAppService _vehicleAppService;

    public VehicleController(IVehicleAppService vehicleAppService)
    {
        _vehicleAppService = vehicleAppService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<ActionResult> EditModal(long vehicleId)
    {
        var vehicle = await _vehicleAppService.GetAsync(new EntityDto<long>(vehicleId));
        var model = new EditVehicleViewModel
        {
            Vehicle = vehicle
        };

        return PartialView("_EditModal", model);
    }
}
