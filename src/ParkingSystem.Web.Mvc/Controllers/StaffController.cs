using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Authorization.Users;
using ParkingSystem.Controllers;
using ParkingSystem.Entities;
using ParkingSystem.Staffs;
using ParkingSystem.Web.Models.Staff;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParkingSystem.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Staffs)]
public class StaffController : ParkingSystemControllerBase
{
    private readonly IStaffAppService _staffAppService;
    private readonly UserManager _userManager;
    private readonly IRepository<Staff, long> _staffRepository;

    public StaffController(
        IStaffAppService staffAppService,
        UserManager userManager,
        IRepository<Staff, long> staffRepository)
    {
        _staffAppService = staffAppService;
        _userManager = userManager;
        _staffRepository = staffRepository;
    }

    public async Task<IActionResult> Index()
    {
        var isManager = await IsGrantedAsync(PermissionNames.Pages_Staffs_Manager);
        var availableUsers = new List<StaffUserLookupDto>();

        if (!isManager)
        {
            return RedirectToAction(nameof(Profile));
        }
       
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            if (staffUsers == null || staffUsers.Count == 0)
            {
                staffUsers = await _userManager.GetUsersInRoleAsync("STAFF");
            }

            var existingStaffUserIds = await _staffRepository.GetAll()
                .Select(s => s.UserId)
                .ToListAsync();

            availableUsers = staffUsers
                .Where(u => !existingStaffUserIds.Contains(u.Id))
                .Select(u => new StaffUserLookupDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    FullName = u.FullName,
                    EmailAddress = u.EmailAddress,
                    PhoneNumber = u.PhoneNumber
                })
                .OrderBy(u => u.FullName)
                .ToList();
        

        var model = new StaffListViewModel
        {
            AvailableUsers = availableUsers,
            IsManager = isManager
        };

        return View(model);
    }

    public async Task<ActionResult> EditModal(long staffId)
    {
        var staff = await _staffAppService.GetAsync(new EntityDto<long>(staffId));
        var model = new EditStaffViewModel
        {
            Staff = staff
        };

        return PartialView("_EditModal", model);
    }

    public async Task<IActionResult> Profile()
    {
        var staff = await _staffAppService.GetMyProfileAsync();

        return View(staff);
    }
}
