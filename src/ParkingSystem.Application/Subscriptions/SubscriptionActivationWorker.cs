using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.Repositories;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Abp.Timing;
using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using System;

namespace ParkingSystem.Subscriptions;

/// <summary>
/// Background worker that periodically checks for subscriptions in paidWaiting status
/// and activates them to inUse when their StartTime has arrived,
/// and cancels pending subscriptions whose payment has expired.
/// </summary>
public class SubscriptionActivationWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly IRepository<Subscription, long> _subscriptionRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IConfiguration _configuration;
    private readonly bool _isEnabled;
    private readonly int _batchSize;
    private const int DefaultIntervalSeconds = 60;
    private const int DefaultBatchSize = 50;

    public SubscriptionActivationWorker(
        AbpTimer timer,
        IRepository<Subscription, long> subscriptionRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IConfiguration configuration)
        : base(timer)
    {
        _subscriptionRepository = subscriptionRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _configuration = configuration;

        _isEnabled = _configuration.GetValue<bool>("BackgroundJobs:SubscriptionActivationWorker:IsEnabled", true);

        var intervalSeconds = _configuration.GetValue<int>("BackgroundJobs:SubscriptionActivationWorker:IntervalSeconds", DefaultIntervalSeconds);
        if (intervalSeconds <= 0)
        {
            intervalSeconds = DefaultIntervalSeconds;
        }
        var batchSize = _configuration.GetValue<int>("BackgroundJobs:SubscriptionActivationWorker:BatchSize", DefaultBatchSize);
        _batchSize = batchSize > 0 ? batchSize : DefaultBatchSize;

        Timer.Period = intervalSeconds * 1000;
    }

    protected override void DoWork()
    {
        if (!_isEnabled)
        {
            Logger.Info("[SubscriptionActivationWorker] Worker is disabled by configuration. Skipping cycle.");
            return;
        }

        try
        {
            var now = Clock.Now;
            using (var uow = _unitOfWorkManager.Begin())
            {
                var dbContext = _subscriptionRepository.GetDbContext();

                // 1. Kích hoạt sang inUse nếu đã đến StartTime và chưa hết hạn
                var activatedCount = dbContext.Database.ExecuteSqlInterpolated($@"
                    UPDATE TOP ({_batchSize}) Subscriptions
                    SET    Status = {(int)SubscriptionStatus.inUse},
                           LastModificationTime = GETUTCDATE()
                    WHERE  Status = {(int)SubscriptionStatus.awaitUsing}
                      AND  StartTime <= {now}
                      AND  EndTime > {now};
                ");

                // 2. Hết hạn khi gói inUse và đã đến thời gian EndTime
                var expiredCount = dbContext.Database.ExecuteSqlInterpolated($@"
                    UPDATE TOP ({_batchSize}) Subscriptions
                    SET    Status = {(int)SubscriptionStatus.expired},
                           LastModificationTime = GETUTCDATE()
                    WHERE  Status = {(int)SubscriptionStatus.inUse}
                      AND  EndTime <= {now};
                ");
                // 3. Hủy subscription nếu Payment đã Expired hoặc Payment đang Pending nhưng đã quá hạn
                var canceledCount = dbContext.Database.ExecuteSqlInterpolated($@"
                    UPDATE TOP ({_batchSize}) s
                    SET    s.Status = {(int)SubscriptionStatus.canceled},
                           s.LastModificationTime = GETUTCDATE()
                    FROM   Subscriptions s
                    INNER JOIN Payments p ON p.SubscriptionId = s.Id
                    WHERE  s.Status = {(int)SubscriptionStatus.pending}
                      AND  (
                          p.PaymentStatus = {(int)PaymentStatus.Expired}
                          OR (p.PaymentStatus = {(int)PaymentStatus.Pending} AND p.ExpiresAt <= {now})
                      )
                      AND  NOT EXISTS (
                          SELECT 1 FROM Payments p2
                          WHERE  p2.SubscriptionId = s.Id
                            AND  p2.PaymentStatus = {(int)PaymentStatus.Pending}
                            AND  p2.ExpiresAt > {now}
                      );
                ");

                uow.Complete();

                if (activatedCount > 0 || canceledCount > 0 || expiredCount > 0)
                {
                    Logger.Info($"[SubscriptionActivationWorker] Cycle finished: Activated={activatedCount}, Canceled={canceledCount}, Expired = {expiredCount}.");
                }
                else
                {
                    Logger.Debug("[SubscriptionActivationWorker] No subscriptions to process in this cycle.");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[SubscriptionActivationWorker] Unexpected error in worker cycle: {ex.Message}", ex);
        }
    }
}
