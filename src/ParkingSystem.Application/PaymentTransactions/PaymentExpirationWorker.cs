using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.Repositories;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Abp.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using System;

namespace ParkingSystem.PaymentTransactions;

/// <summary>
/// Background worker tự động xử lý Payment hết hạn:
/// 1. Đánh dấu tất cả PaymentTransaction đang Pending thuộc Payment hết hạn → Failed.
/// 2. Chuyển Payment từ Pending → Expired khi ExpiresAt &lt;= now.
/// 3. Chuyển Subscription từ pending → canceled nếu Payment của gói đó bị Expired.
/// 4. Trả trạng thái ParkingSession từ PendingPayment → Active nếu Payment của phiên đó hết hạn (để có thể checkout lại).
/// Sử dụng raw SQL trong cùng một UoW để đảm bảo atomicity và hiệu năng cao.
/// </summary>
public class PaymentExpirationWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly IRepository<Payment, long> _paymentRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IConfiguration _configuration;
    private readonly bool _isEnabled;
    private readonly int _batchSize;
    private const int DefaultIntervalSeconds = 60;
    private const int DefaultBatchSize = 100;

    public PaymentExpirationWorker(
        AbpTimer timer,
        IRepository<Payment, long> paymentRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IConfiguration configuration)
        : base(timer)
    {
        _paymentRepository = paymentRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _configuration = configuration;

        _isEnabled = _configuration.GetValue<bool>("BackgroundJobs:PaymentExpirationWorker:IsEnabled", true);

        var intervalSeconds = _configuration.GetValue<int>("BackgroundJobs:PaymentExpirationWorker:IntervalSeconds", DefaultIntervalSeconds);
        if (intervalSeconds <= 0)
            intervalSeconds = DefaultIntervalSeconds;

        var batchSize = _configuration.GetValue<int>("BackgroundJobs:PaymentExpirationWorker:BatchSize", DefaultBatchSize);
        _batchSize = batchSize > 0 ? batchSize : DefaultBatchSize;

        Timer.Period = intervalSeconds * 1000;
    }

    protected override void DoWork()
    {
        if (!_isEnabled)
        {
            Logger.Info("[PaymentExpirationWorker] Worker is disabled by configuration. Skipping cycle.");
            return;
        }

        try
        {
            var now = Clock.Now;

            using (var uow = _unitOfWorkManager.Begin())
            {
                var dbContext = _paymentRepository.GetDbContext();

                // Bước 1: Fail toàn bộ PaymentTransaction đang Pending thuộc về Payment đã hết hạn.
                // Chạy TRƯỚC bước expire Payment để tránh race condition:
                // nếu chuyển Payment → Expired trước, RetryPayment sẽ bị chặn nhưng
                // Transaction cũ vẫn còn Pending → inconsistent.
                var failedTxCount = dbContext.Database.ExecuteSqlInterpolated($@"
                    UPDATE pt
                    SET    pt.[Status]             = {(int)TransactionStatus.Failed},
                           pt.TransactionTime      = GETUTCDATE(),
                           pt.LastModificationTime = GETUTCDATE()
                    FROM   PaymentTransactions AS pt
                    INNER JOIN Payments AS p ON p.Id = pt.PaymentId
                    WHERE  pt.[Status]     = {(int)TransactionStatus.Pending}
                      AND  p.PaymentStatus = {(int)PaymentStatus.Pending}
                      AND  p.ExpiresAt    <= {now};
                ");

                // Bước 2: Chuyển Payment Pending đã hết hạn → Expired (xử lý theo batch).
                var expiredPaymentCount = dbContext.Database.ExecuteSqlInterpolated($@"
                    UPDATE TOP ({_batchSize}) Payments
                    SET    PaymentStatus        = {(int)PaymentStatus.Expired},
                           LastModificationTime = GETUTCDATE()
                    WHERE  PaymentStatus = {(int)PaymentStatus.Pending}
                      AND  ExpiresAt   <= {now};
                ");

                // Bước 3: Hủy Subscription (pending → canceled) nếu Payment của gói đó đã Expired và không còn Payment Pending nào còn hạn.
                var canceledSubCount = dbContext.Database.ExecuteSqlInterpolated($@"
                    UPDATE s
                    SET    s.Status = {(int)SubscriptionStatus.canceled},
                           s.LastModificationTime = GETUTCDATE()
                    FROM   Subscriptions AS s
                    INNER JOIN Payments AS p ON p.SubscriptionId = s.Id
                    WHERE  s.Status = {(int)SubscriptionStatus.pending}
                      AND  p.PaymentStatus = {(int)PaymentStatus.Expired}
                      AND  NOT EXISTS (
                          SELECT 1 FROM Payments AS p2
                          WHERE p2.SubscriptionId = s.Id
                            AND p2.PaymentStatus = {(int)PaymentStatus.Pending}
                            AND p2.ExpiresAt > {now}
                      );
                ");

                // Bước 4: Trả ParkingSession từ PendingPayment về lại Active nếu Payment của nó hết hạn (để có thể checkout lại).
                var resetSessionCount = dbContext.Database.ExecuteSqlInterpolated($@"
                    UPDATE s
                    SET    s.Status = {(int)ParkingSessionStatus.Active},
                           s.LastModificationTime = GETUTCDATE()
                    FROM   ParkingSessions AS s
                    INNER JOIN Payments AS p ON p.ParkingSessionId = s.Id
                    WHERE  s.Status = {(int)ParkingSessionStatus.PendingPayment}
                      AND  p.PaymentStatus = {(int)PaymentStatus.Expired}
                      AND  NOT EXISTS (
                          SELECT 1 FROM Payments AS p2
                          WHERE p2.ParkingSessionId = s.Id
                            AND p2.PaymentStatus = {(int)PaymentStatus.Pending}
                            AND p2.ExpiresAt > {now}
                      );
                ");

                uow.Complete();

                if (expiredPaymentCount > 0 || failedTxCount > 0 || canceledSubCount > 0 || resetSessionCount > 0)
                {
                    Logger.Info($"[PaymentExpirationWorker] Cycle finished: ExpiredPayments={expiredPaymentCount}, FailedTransactions={failedTxCount}, CanceledSubscriptions={canceledSubCount}, ResetSessions={resetSessionCount}.");
                }
                else
                {
                    Logger.Debug("[PaymentExpirationWorker] No expired payments found in this cycle.");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[PaymentExpirationWorker] Unexpected error in worker cycle: {ex.Message}", ex);
        }
    }
}
