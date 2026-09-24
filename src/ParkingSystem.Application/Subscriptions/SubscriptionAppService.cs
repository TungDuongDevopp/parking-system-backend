using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using ParkingSystem.Helpers;
using ParkingSystem.Subscriptions.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;



namespace ParkingSystem.Subscriptions;

[AbpAuthorize(PermissionNames.Pages_Subscriptions)]
public class SubscriptionAppService : ParkingSystemAppServiceBase,ISubscriptionAppService
{
    private readonly IRepository<Customer, long> _customerRepository;
    private readonly IRepository<Quotation, long> _quotationRepository;
    private readonly IRepository<Subscription, long> _repository;

    public SubscriptionAppService(IRepository<Subscription,long> repository, IRepository<Quotation, long> quotationRepository, IRepository<Customer, long> customerRepository)
    {
        _customerRepository = customerRepository;
        _quotationRepository = quotationRepository;
        _repository = repository;

    }
    private async Task CheckSubcriptionViewAccessAsync(Subscription subscription)
    {
        var canViewAll = await PermissionChecker.IsGrantedAsync(
            PermissionNames.Pages_Subscriptions_Manager
        );

        if (canViewAll)
            return;

        var userId = AbpSession.UserId;   

        var customer = await _customerRepository.FirstOrDefaultAsync(x => x.Id == subscription.CustomerId);
        if (customer == null || customer.UserId != userId)
        {
            throw new AbpAuthorizationException("You do not have permission to view this subcription.");
        }
    }
    public async Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto input)
    {
        var userId = AbpSession.UserId;

        var customer = await _customerRepository
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (customer == null)
            throw new UserFriendlyException("Customer profile not found");

        var currentSubscription = await _repository.FirstOrDefaultAsync(
     x => x.CustomerId == customer.Id &&
          (x.Status == SubscriptionStatus.pending ||
           x.Status == SubscriptionStatus.inUse));

        if (currentSubscription != null)
        {
            throw new BusinessRuleException(
                "You already have an active subscription.");
        }


        var quotation = await _quotationRepository
            .FirstOrDefaultAsync(x => x.Id == input.QuotationId);

        if (quotation == null)
            throw new ResourceNotFoundException(
                "Quotation not found with id: " + input.QuotationId);

        if (quotation.DurationUnit == DurationUnit.Hour ||
            quotation.DurationUnit == DurationUnit.Day)
        {
            throw new BusinessRuleException("You can not buy this subscription");
        }

        var subscription = new Subscription
        {
            CustomerId = customer.Id,
            QuotationId = quotation.Id,
            StartTime = input.StartTime,
            EndTime = quotation.DurationUnit switch
            {
                DurationUnit.Week =>
                    input.StartTime.AddDays(quotation.Duration * 7),

                DurationUnit.Month =>
                    input.StartTime.AddMonths(quotation.Duration),

                DurationUnit.Year =>
                    input.StartTime.AddYears(quotation.Duration),

                _ => throw new UserFriendlyException("Invalid duration unit")
            }
        };

        var created = await _repository.InsertAsync(subscription);

        await CurrentUnitOfWork.SaveChangesAsync();

        return ObjectMapper.Map<SubscriptionDto>(created);
    }

    public async Task<PagedResultDto<SubscriptionDto>> GetAllAsync(
     PagedSubscriptionResultRequestDto input)
    {
        IQueryable<Subscription> query = _repository.GetAll()
            .AsNoTracking()
            .Include(x => x.Customer);

        // Permission / Ownership
        var canViewAll =
            PermissionChecker.IsGranted(
                PermissionNames.Pages_Subscriptions_Manager);

        if (!canViewAll)
        {
            var userId = AbpSession.UserId
                ?? throw new AbpAuthorizationException(
                    "User is not logged in.");

            query = query.Where(x => x.Customer.UserId == userId);
        }

        // Filter
        query = query
            .WhereIf(
                input.Status.HasValue,
                x => x.Status == input.Status)
            .WhereIf(
                input.CustomerId.HasValue,
                x => x.CustomerId == input.CustomerId)
            .WhereIf(
                input.QuotationId.HasValue,
                x => x.QuotationId == input.QuotationId)
            .WhereIf(
                input.StartTime.HasValue,
                x => x.StartTime >= input.StartTime)
            .WhereIf(
                input.EndTime.HasValue,
                x => x.EndTime <= input.EndTime);

        // Sorting
        if (!string.IsNullOrEmpty(input.Sorting))
        {
            var sorting = SortingHelper.ValidateSorting(
                input.Sorting,
                nameof(Subscription.Id),
                nameof(Subscription.CustomerId),
                nameof(Subscription.StartTime),
                nameof(Subscription.EndTime),
                nameof(Subscription.CreationTime),
                nameof(Subscription.QuotationId),
                nameof(Subscription.Status)
            );

            query = query.OrderBy(sorting);
        }
        else
        {
            query = query.OrderByDescending(x => x.Id);
        }

        // Total count
        var totalCount = await query.CountAsync();

        // Paging
        var items = await query
            .PageBy(input.SkipCount, input.MaxResultCount)
            .ToListAsync();

        // Mapping
        var result = ObjectMapper.Map<List<SubscriptionDto>>(items);

        return new PagedResultDto<SubscriptionDto>(
            totalCount,
            result);
    }

    public async Task<SubscriptionDto> GetAsync(EntityDto<long> input)
    {
        var subscription = await _repository.FirstOrDefaultAsync(input.Id);

        if (subscription == null)
        {
            throw new ResourceNotFoundException("Subcription not found with id: " + input.Id);
        }

        await CheckSubcriptionViewAccessAsync(subscription);

        return ObjectMapper.Map<SubscriptionDto>(subscription);
    }
}
