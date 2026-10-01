using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Authorization;
using ParkingSystem.Controllers;
using ParkingSystem.Customers;
using ParkingSystem.Web.Models.Customer;
using System.Threading.Tasks;

namespace ParkingSystem.Web.Controllers
{
    [AbpMvcAuthorize(PermissionNames.Pages_Customers)]
    public class CustomerController : ParkingSystemControllerBase
    {
        private readonly ICustomerAppService _customerAppService;

        public CustomerController(ICustomerAppService customerAppService)
        {
            _customerAppService = customerAppService;
        }

        public async Task<IActionResult> Index()
        {
            if (!await IsGrantedAsync(PermissionNames.Pages_Customers_ViewAll))
            {
                return RedirectToAction(nameof(Profile));
            }

            return View();
        }

        public async Task<IActionResult> Profile()
        {
            var customer = await _customerAppService.GetMyProfileAsync();
            if (customer == null)
            {
                return RedirectToAction(nameof(CreateProfile));
            }
            return View(customer);
        }

        public IActionResult CreateProfile()
        {
            return View();
        }

        public async Task<ActionResult> EditModal(long customerId)
        {
            var customer = await _customerAppService.GetAsync(new EntityDto<long>(customerId));
            var model = new EditCustomerViewModel
            {
                Customer = customer
            };

            return PartialView("_EditModal", model);
        }
    }
}
