using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Authorization;
using ParkingSystem.Controllers;
using ParkingSystem.Subscriptions;
using System.Threading.Tasks;


namespace ParkingSystem.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Subscriptions)]
public class SubscriptionController : ParkingSystemControllerBase
{
    private readonly ISubscriptionAppService _subscriptionAppService;

    public SubscriptionController(ISubscriptionAppService subscriptionAppService)
    {
        _subscriptionAppService = subscriptionAppService;
    }

    /// <summary>
    /// Admin / manager view — the existing DataTable listing.
    /// </summary>
    public IActionResult Index()
    {
        return View();
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
