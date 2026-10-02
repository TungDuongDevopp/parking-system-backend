using Abp.AspNetCore.Mvc.Authorization;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Controllers;
using ParkingSystem.Entities;
using ParkingSystem.Web.Models.Subscription;
using System.Linq;
using System.Threading.Tasks;


namespace ParkingSystem.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Subscriptions)]
public class SubscriptionController : ParkingSystemControllerBase
{
    private readonly ISubscriptionAppService _subscriptionAppService;

    public SubscriptionController(
        ISubscriptionAppService subscriptionAppService)
    {
        _subscriptionAppService = subscriptionAppService;
    }

    /// <summary>
    /// Admin / manager view — the existing DataTable listing.
    /// </summary>
    public async Task<IActionResult> Index()
    {
      

        var canViewAll = await IsGrantedAsync(PermissionNames.Pages_Subscriptions_Manager);
        if (!canViewAll)
        {
           return RedirectToAction(nameof(MySubscription));
        }

       var subscriptionInfo = await _subscriptionAppService.GetSubscriptionLookUpAsync();

        var model = new SubscriptionListViewModel
        {
            Customers = subscriptionInfo.Customers,
            Quotations = subscriptionInfo.Quotations
        };

        return View(model);
    }

    /// <summary>
    /// Customer-facing "My Subscription" page.
    /// Managers/admins who land here are redirected to the admin Index.
    /// </summary>
   
    public async Task<IActionResult> MySubscription()
    {
        if (await IsGrantedAsync(PermissionNames.Pages_Subscriptions_Manager))
        {
            return RedirectToAction(nameof(Index));
        }

        return View();
    }
}
