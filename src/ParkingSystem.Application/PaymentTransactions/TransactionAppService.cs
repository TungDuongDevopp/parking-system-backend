

using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.EntityFrameworkCore.Repositories;
using Abp.Linq.Extensions;
using Abp.Timing;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using ParkingSystem.Helpers;
using ParkingSystem.PaymentTransactions.Dto;
using ParkingSystem.Subscriptions.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace ParkingSystem.PaymentTransactions;

[AbpAuthorize(PermissionNames.Pages_Transaction)]
public class TransactionAppService : ParkingSystemAppServiceBase, ITransactionAppService
{
    private readonly IRepository<PaymentTransaction, long> _repository;
    private readonly TransactionManager _manager;
    private readonly IRepository<Subscription, long> _subscriptionRepository;
    private readonly IRepository<Payment, long> _paymentRepository;

    public TransactionAppService(
        IRepository<PaymentTransaction, long> repository,
        TransactionManager manager,
        IRepository<Subscription, long> subscriptionRepository,
        IRepository<Payment, long> paymentRepository)
    {
        _repository = repository;
        _manager = manager;
        _subscriptionRepository = subscriptionRepository;
        _paymentRepository = paymentRepository;
    }

    public async Task<PagedResultDto<TransactionDto>> GetAllAsync(PagedTransactionResultRequest input)
    {
        IQueryable<PaymentTransaction> query = _repository.GetAll()
            .AsNoTracking()
            .Include(x => x.Payment)
                .ThenInclude(p => p.Subscription)
                    .ThenInclude(s => s.Customer)
            .Include(x => x.Payment)
                .ThenInclude(p => p.ParkingSession)
                    .ThenInclude(ps => ps.Customer);

        var keyword = input.Keyword?.Trim();

        query = query
            .WhereIf(
                !string.IsNullOrWhiteSpace(keyword),
                x => (x.TransactionCode != null && x.TransactionCode.Contains(keyword))
                    || (x.Payment != null && x.Payment.Description != null && x.Payment.Description.Contains(keyword))
                    || (x.Payment != null && x.Payment.ParkingSession != null && x.Payment.ParkingSession.TicketCode != null && x.Payment.ParkingSession.TicketCode.Contains(keyword))
                    || (x.Payment != null && x.Payment.ParkingSession != null && x.Payment.ParkingSession.PlateNumber != null && x.Payment.ParkingSession.PlateNumber.Contains(keyword))
                    || (x.Payment != null && x.Payment.ParkingSession != null && x.Payment.ParkingSession.Customer != null && x.Payment.ParkingSession.Customer.Name != null && x.Payment.ParkingSession.Customer.Name.Contains(keyword))
                    || (x.Payment != null && x.Payment.Subscription != null && x.Payment.Subscription.Customer != null && x.Payment.Subscription.Customer.Name != null && x.Payment.Subscription.Customer.Name.Contains(keyword))
            )
            .WhereIf(
                input.PaymentId.HasValue,
                x => x.PaymentId == input.PaymentId.Value
            )
            .WhereIf(
                input.MinAmount.HasValue,
                x => x.Amount >= input.MinAmount.Value
            )
            .WhereIf(
                input.MaxAmount.HasValue,
                x => x.Amount <= input.MaxAmount.Value
            )
            .WhereIf(
                input.Status.HasValue,
                x => x.Status == input.Status.Value
            )
            .WhereIf(
                input.Type.HasValue,
                x => x.Type == input.Type.Value
            )
            .WhereIf(
                input.TransactionTimeFrom.HasValue,
                x => x.TransactionTime.HasValue && x.TransactionTime >= input.TransactionTimeFrom.Value
            )
            .WhereIf(
                input.TransactionTimeTo.HasValue,
                x => x.TransactionTime.HasValue && x.TransactionTime <= input.TransactionTimeTo.Value
            );

        var totalCount = await query.CountAsync();

        if (!string.IsNullOrEmpty(input.Sorting))
        {
            var sorting = SortingHelper.ValidateSorting(
                input.Sorting,
                nameof(PaymentTransaction.Id),
                nameof(PaymentTransaction.TransactionCode),
                nameof(PaymentTransaction.Amount),
                nameof(PaymentTransaction.Status),
                nameof(PaymentTransaction.Type),
                nameof(PaymentTransaction.TransactionTime),
                nameof(PaymentTransaction.CreationTime),
                nameof(PaymentTransaction.PaymentId)
            );
            query = query.OrderBy(sorting);
        }
        else
        {
            query = query.OrderByDescending(x => x.Id);
        }

        var items = await query
            .PageBy(input.SkipCount, input.MaxResultCount)
            .ToListAsync();

        var result = ObjectMapper.Map<List<TransactionDto>>(items);

        return new PagedResultDto<TransactionDto>(totalCount, result);
    }

    public async Task<TransactionDto> GetAsync(EntityDto<long> input)
    {
        var transaction = await _repository.GetAll()
            .AsNoTracking()
            .Include(x => x.Payment)
                .ThenInclude(p => p.Subscription)
                    .ThenInclude(s => s.Customer)
            .Include(x => x.Payment)
                .ThenInclude(p => p.ParkingSession)
                    .ThenInclude(ps => ps.Customer)
            .FirstOrDefaultAsync(x => x.Id == input.Id);

        if (transaction == null)
        {
            throw new ResourceNotFoundException("Transaction not found with id: " + input.Id);
        }

        return ObjectMapper.Map<TransactionDto>(transaction);
    }

    public async Task<TransactionLookupDto> GetTransactionLookupAsync()
    {
        var subQuery = _repository.GetAll().AsNoTracking();
        var paymentIds = subQuery.Select(p => p.PaymentId).Distinct();
        var payments = await _paymentRepository.GetAll()
           .AsNoTracking()
           .Where(p => paymentIds.Contains(p.Id))
           .OrderBy(p => p.Id)
           .Select(c => new TransactionPaymentLookupDto
           {
               Id = c.Id,
               Type= c.PaymentType.ToString(),
               Method = c.PaymentMethod.ToString()
           })
           .ToListAsync();
        return new TransactionLookupDto
        {
            Payments = payments
        };
    }

    public async Task<TransactionDto> ProcessPaymentResultAsync(ProcessPaymentResultInput input)
    {
        var transaction = await _repository.GetAll()
            .Include(x => x.Payment)
            .FirstOrDefaultAsync(x => x.TransactionCode == input.TransactionCode);

        if (transaction == null)
        {
            throw new ResourceNotFoundException("Transaction not found with code: " + input.TransactionCode);
        }

        if (!input.IsSuccess)
        {
            await _manager.FailedAsync(transaction);
            transaction.Status = TransactionStatus.Failed;
            transaction.TransactionTime = Clock.Now;
            return ObjectMapper.Map<TransactionDto>(transaction);
        }

        // IsSuccess == true
        await _manager.CompleteAsync(transaction);
        transaction.Status = TransactionStatus.Success;
        transaction.TransactionTime = Clock.Now;

        // Nếu Payment thuộc loại Subscription, điều phối cập nhật trạng thái Subscription
        if (transaction.Payment.PaymentType == PaymentType.subscription)
        {
            if (!transaction.Payment.SubscriptionId.HasValue)
            {
                throw new BusinessRuleException("Payment không có SubscriptionId liên kết hợp lệ.");
            }

            var subscriptionId = transaction.Payment.SubscriptionId.Value;
            var subscription = await _subscriptionRepository.FirstOrDefaultAsync(subscriptionId);
            if (subscription == null)
            {
                throw new ResourceNotFoundException("Subscription not found with id: " + subscriptionId);
            }

            // Quy tắc kích hoạt theo B3:
            // - Nếu StartTime <= Clock.Now: chuyển sang inUse (đang sử dụng)
            // - Nếu StartTime > Clock.Now: chuyển sang paidWaiting (đã thanh toán, chờ đến ngày hiệu lực)
            var targetStatus = subscription.StartTime <= Clock.Now
                ? SubscriptionStatus.inUse
                : SubscriptionStatus.paidWaiting;

            var dbContext = _repository.GetDbContext();
            var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE Subscriptions
                SET    Status = {(int)targetStatus},
                       LastModificationTime = GETUTCDATE()
                WHERE  Id = {subscriptionId}
                  AND  Status = {(int)SubscriptionStatus.pending};
            ");

            if (rowsAffected == 0)
            {
                throw new BusinessRuleException(
                    "Không thể cập nhật Subscription: gói không tồn tại hoặc không ở trạng thái pending.");
            }
        }

        return ObjectMapper.Map<TransactionDto>(transaction);
    }

    public async Task<TransactionDto> RetryPaymentAsync(RetryPaymentInput input)
    {
        var payment = await _paymentRepository.FirstOrDefaultAsync(input.PaymentId);

        if (payment == null)
            throw new ResourceNotFoundException("Payment not found with id: " + input.PaymentId);

        if (payment.PaymentStatus != PaymentStatus.Pending)
            throw new BusinessRuleException("Chỉ có thể thử lại thanh toán khi Payment đang ở trạng thái Pending.");

        if (payment.ExpiresAt <= Clock.Now)
            throw new BusinessRuleException("Payment đã hết hạn, không thể tạo giao dịch mới.");

        var newTx = await _manager.CreatePaymentTransactionAsync(payment);

        return ObjectMapper.Map<TransactionDto>(newTx);
    }

    
}
