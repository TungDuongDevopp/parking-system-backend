using Abp.Application.Services.Dto;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Controllers;
using ParkingSystem.Entities;
using ParkingSystem.Quotations;
using ParkingSystem.Web.Models.Quotation;
using System.Threading.Tasks;

using Abp.AspNetCore.Mvc.Authorization;
using ParkingSystem.Authorization;

namespace ParkingSystem.Web.Controllers
{
    [AbpMvcAuthorize(PermissionNames.Pages_Quotations)]
    public class QuotationController : ParkingSystemControllerBase
    {
        private readonly IQuotationAppService _quotationAppService;

        public QuotationController(IQuotationAppService quotationAppService)
        {
            _quotationAppService = quotationAppService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> EditModal(long quotationId)
        {
            var quotation = await _quotationAppService.GetAsync(new EntityDto<long>(quotationId));
            var model = new EditQuotationViewModel
            {
                Quotation = quotation
            };
            return PartialView("_EditModal", model);
        }
    }
}
