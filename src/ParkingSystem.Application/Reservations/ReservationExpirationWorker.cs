using Abp.Dependency;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Castle.Core.Logging;
using Microsoft.Extensions.Configuration;
using System;

namespace ParkingSystem.Reservations;

/// <summary>
/// Background worker that periodically detects and expires reservations past their deadline,
/// releasing held parking spots and capacities atomically.
/// </summary>
public class ReservationExpirationWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ReservationManager _reservationManager;
    private readonly IConfiguration _configuration;
    private readonly bool _isEnabled;
    private readonly int _batchSize;
    private const int DefaultIntervalSeconds = 60;
    private const int DefaultBatchSize = 50;

    public ReservationExpirationWorker(
        AbpTimer timer,
        ReservationManager reservationManager,
        IConfiguration configuration)
        : base(timer)
    {
        _reservationManager = reservationManager;
        _configuration = configuration;

        _isEnabled = _configuration.GetValue<bool>("BackgroundJobs:ReservationExpirationWorker:IsEnabled", true);

        var intervalSeconds = _configuration.GetValue<int>("BackgroundJobs:ReservationExpirationWorker:IntervalSeconds", DefaultIntervalSeconds);
        if (intervalSeconds <= 0)
        {
            intervalSeconds = DefaultIntervalSeconds;
        }

        var batchSize = _configuration.GetValue<int>("BackgroundJobs:ReservationExpirationWorker:BatchSize", DefaultBatchSize);
        _batchSize = batchSize > 0 ? batchSize : DefaultBatchSize;

        Timer.Period = intervalSeconds * 1000;
    }

    protected override void DoWork()
    {
        if (!_isEnabled)
        {
            Logger.Info("[ReservationExpirationWorker] Worker is disabled by configuration (BackgroundJobs:ReservationExpirationWorker:IsEnabled = false). Skipping cycle.");
            return;
        }

        try
        {
            Logger.Info($"[ReservationExpirationWorker] Expiration cycle started (BatchSize={_batchSize}).");

            var result = AsyncHelper.RunSync(() => _reservationManager.ExpireExpiredReservationsBatchAsync(_batchSize));

            if (result.TotalFound > 0)
            {
                Logger.Info($"[ReservationExpirationWorker] Cycle finished: Found={result.TotalFound}, Success={result.SuccessCount}, Skipped={result.SkippedCount}, Failed={result.FailedCount}.");

                foreach (var detail in result.Details)
                {
                    if (detail.IsSuccess)
                    {
                        Logger.Info($"[ReservationExpirationWorker] Successfully expired Reservation Id={detail.ReservationId}.");
                    }
                    else if (detail.IsSkipped)
                    {
                        Logger.Warn($"[ReservationExpirationWorker] Skipped Reservation Id={detail.ReservationId}. Reason: {detail.Message}");
                    }
                    else
                    {
                        Logger.Error($"[ReservationExpirationWorker] Failed to expire Reservation Id={detail.ReservationId}. Error: {detail.ErrorMessage}", detail.Exception);
                    }
                }
            }
            else
            {
                Logger.Debug("[ReservationExpirationWorker] No expired reservations found in this cycle.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[ReservationExpirationWorker] Unexpected error in worker cycle: {ex.Message}", ex);
        }
    }
}
