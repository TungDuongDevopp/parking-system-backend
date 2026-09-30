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
    private readonly IRepository<Customer, long> _customerRepository;
    private readonly IRepository<Quotation, long> _quotationRepository;
    private readonly IRepository<Subscription, long> _subscriptionRepository;

    public SubscriptionController(
        ISubscriptionAppService subscriptionAppService,
        IRepository<Customer, long> customerRepository,
        IRepository<Quotation, long> quotationRepository,
        IRepository<Subscription, long> subscriptionRepository)
    {
        _subscriptionAppService = subscriptionAppService;
        _customerRepository = customerRepository;
        _quotationRepository = quotationRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    /// <summary>
    /// Admin / manager view — the existing DataTable listing.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var subQuery = _subscriptionRepository.GetAll().AsNoTracking();

        var canViewAll = await IsGrantedAsync(PermissionNames.Pages_Subscriptions_Manager);
        if (!canViewAll)
        {
            var userId = AbpSession.UserId;
            if (userId.HasValue)
            {
                subQuery = subQuery.Where(x => x.Customer.UserId == userId.Value);
            }
        }

        var customerIds = subQuery.Select(s => s.CustomerId).Distinct();
        var quotationIds = subQuery.Select(s => s.QuotationId).Distinct();

        var customers = await _customerRepository.GetAll()
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .Select(c => new SubscriptionCustomerLookupDto
            {
                Id = c.Id,
                Name = c.Name,
                PhoneNumber = c.PhoneNumber
            })
            .ToListAsync();

        var quotations = await _quotationRepository.GetAll()
            .AsNoTracking()
            .Where(q => quotationIds.Contains(q.Id))
            .OrderBy(q => q.VehicleType)
            .ThenBy(q => q.DurationUnit)
            .ThenBy(q => q.Duration)
            .Select(q => new SubscriptionQuotationLookupDto
            {
                Id = q.Id,
                VehicleType = q.VehicleType,
                Duration = q.Duration,
                DurationUnit = q.DurationUnit,
                Price = q.Price
            })
            .ToListAsync();

        var model = new SubscriptionListViewModel
        {
            Customers = customers,
            Quotations = quotations
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
