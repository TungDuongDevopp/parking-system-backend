

using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Entities;
using ParkingSystem.Exceptions;
using ParkingSystem.Helpers;
using ParkingSystem.Payments.Dto;
using ParkingSystem.PaymentTransactions;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;


namespace ParkingSystem.Payments;

[AbpAuthorize(PermissionNames.Pages_Payment)]
public class PaymentAppService : ParkingSystemAppServiceBase, IPaymentAppService
{
    private readonly IRepository<Payment, long> _repository;
    public PaymentAppService(IRepository<Payment, long> repository)
    {
        _repository = repository;
    }

    [AbpAuthorize(PermissionNames.Pages_Payment_Manager)]
    public async Task<PagedResultDto<PaymentDto>> GetAllAsync(PagedPaymentResultRequestDto input)
    {
        IQueryable<Payment> query = _repository.GetAll()
            .AsNoTracking()
            .Include(x => x.Subscription)
                .ThenInclude(s => s.Customer)
            .Include(x => x.ParkingSession)
                .ThenInclude(ps => ps.Customer);

        var keyword = input.Keyword?.Trim();

        query = query
            .WhereIf(
                !string.IsNullOrWhiteSpace(keyword),
                x => (x.Description != null && x.Description.Contains(keyword))
                    || (x.ParkingSession != null && x.ParkingSession.TicketCode != null && x.ParkingSession.TicketCode.Contains(keyword))
                    || (x.ParkingSession != null && x.ParkingSession.PlateNumber != null && x.ParkingSession.PlateNumber.Contains(keyword))
                    || (x.Subscription != null && x.Subscription.Customer != null && x.Subscription.Customer.Name != null && x.Subscription.Customer.Name.Contains(keyword))
                    || (x.ParkingSession != null && x.ParkingSession.Customer != null && x.ParkingSession.Customer.Name != null && x.ParkingSession.Customer.Name.Contains(keyword))
            )
            .WhereIf(
                input.PaymentMethod.HasValue,
                x => x.PaymentMethod == input.PaymentMethod.Value
            )
            .WhereIf(
                input.PaymentStatus.HasValue,
                x => x.PaymentStatus == input.PaymentStatus.Value
            )
            .WhereIf(
                input.PaymentType.HasValue,
                x => x.PaymentType == input.PaymentType.Value
            )
            .WhereIf(
                input.SubscriptionId.HasValue,
                x => x.SubscriptionId == input.SubscriptionId.Value
            )
            .WhereIf(
                input.ParkingSessionId.HasValue,
                x => x.ParkingSessionId == input.ParkingSessionId.Value
            )
            .WhereIf(
                input.AmountFrom.HasValue,
                x => x.Amount >= input.AmountFrom.Value
            )
            .WhereIf(
                input.AmountTo.HasValue,
                x => x.Amount <= input.AmountTo.Value
            )
            .WhereIf(
                input.ExpiresFrom.HasValue,
                x => x.ExpiresAt >= input.ExpiresFrom.Value
            )
            .WhereIf(
                input.ExpiresTo.HasValue,
                x => x.ExpiresAt <= input.ExpiresTo.Value
            )
            .WhereIf(
                input.PaidAtFrom.HasValue,
                x => x.PaidAt.HasValue && x.PaidAt >= input.PaidAtFrom.Value
            )
            .WhereIf(
                input.PaidAtTo.HasValue,
                x => x.PaidAt.HasValue && x.PaidAt <= input.PaidAtTo.Value
            );

        // Total count
        var totalCount = await query.CountAsync();

        // Sorting
        if (!string.IsNullOrEmpty(input.Sorting))
        {
            var sorting = SortingHelper.ValidateSorting(
                input.Sorting,
                nameof(Payment.Id),
                nameof(Payment.PaymentMethod),
                nameof(Payment.PaymentStatus),
                nameof(Payment.PaymentType),
                nameof(Payment.Amount),
                nameof(Payment.PaidAt),
                nameof(Payment.ExpiresAt),
                nameof(Payment.CreationTime),
                nameof(Payment.SubscriptionId),
                nameof(Payment.ParkingSessionId)
            );
            query = query.OrderBy(sorting);
        }
        else
        {
            query = query.OrderByDescending(x => x.Id);
        }

        // Paging
        var items = await query
            .PageBy(input.SkipCount, input.MaxResultCount)
            .ToListAsync();

        var result = ObjectMapper.Map<List<PaymentDto>>(items);

        return new PagedResultDto<PaymentDto>(totalCount, result);
    }

    [AbpAuthorize(PermissionNames.Pages_Payment_Manager)]
    public async Task<PaymentDto> GetAsync(EntityDto<long> input)
    {
        var payment = await _repository.GetAll()
            .AsNoTracking()
            .Include(x => x.Subscription)
                .ThenInclude(s => s.Customer)
            .Include(x => x.ParkingSession)
                .ThenInclude(ps => ps.Customer)
            .FirstOrDefaultAsync(x => x.Id == input.Id);

        if (payment == null)
        {
            throw new ResourceNotFoundException("Payment not found with id: " + input.Id);
        }

        return ObjectMapper.Map<PaymentDto>(payment);
    }
}
