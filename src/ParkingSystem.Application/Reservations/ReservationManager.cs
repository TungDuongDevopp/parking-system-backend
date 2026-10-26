using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.Repositories;
using Abp.Timing;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using ParkingSystem.Reservations.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace ParkingSystem.Reservations;

public class ReservationManager : DomainService
{
    private readonly IRepository<Reservation, long> _repository;
    private readonly IRepository<ParkingArea, long> _parkingAreaRepository;
    private readonly IRepository<ParkingSpot, long> _parkingSpotRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ReservationManager(
        IRepository<Reservation, long> repository,
        IRepository<ParkingArea, long> parkingAreaRepository,
        IRepository<ParkingSpot, long> parkingSpotRepository,
        IUnitOfWorkManager unitOfWorkManager = null)
    {
        _repository = repository;
        _parkingAreaRepository = parkingAreaRepository;
        _parkingSpotRepository = parkingSpotRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    private IUnitOfWorkManager UowManager => _unitOfWorkManager ?? UnitOfWorkManager;

    /// <summary>
    /// Processes a batch of candidate expired reservations with isolated transactions per record.
    /// </summary>
    public async Task<BatchExpireResult> ExpireExpiredReservationsBatchAsync(int batchSize)
    {
        var result = new BatchExpireResult();
        var effectiveBatchSize = batchSize > 0 ? batchSize : 50;

        List<long> candidateIds;
        using (var uow = UowManager.Begin())
        {
            var now = Clock.Now;
            candidateIds = await _repository.GetAll()
                .AsNoTracking()
                .Where(x => x.Status == ReservationStatus.Reserved && x.ExpireAt <= now)
                .OrderBy(x => x.ExpireAt)
                .Select(x => x.Id)
                .Take(effectiveBatchSize)
                .ToListAsync();

            await uow.CompleteAsync();
        }

        result.TotalFound = candidateIds.Count;
        if (result.TotalFound == 0)
        {
            return result;
        }

        foreach (var id in candidateIds)
        {
            var itemResult = await ExpireReservationByIdAsync(id);
            result.Details.Add(itemResult);

            if (itemResult.IsSuccess)
            {
                result.SuccessCount++;
            }
            else if (itemResult.IsSkipped)
            {
                result.SkippedCount++;
            }
            else
            {
                result.FailedCount++;
            }
        }

        return result;
    }

    /// <summary>
    /// Expires a single reservation by ID within its own isolated transaction (RequiresNew).
    /// </summary>
    public async Task<ExpireReservationResult> ExpireReservationByIdAsync(long id)
    {
        try
        {
            using (var uow = UowManager.Begin(TransactionScopeOption.RequiresNew))
            {
                var reservation = await _repository.GetAll()
                    .Include(r => r.ParkingArea)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reservation == null)
                {
                    return new ExpireReservationResult
                    {
                        ReservationId = id,
                        IsSkipped = true,
                        Message = "Reservation not found or was deleted."
                    };
                }

                if (reservation.Status != ReservationStatus.Reserved)
                {
                    return new ExpireReservationResult
                    {
                        ReservationId = id,
                        IsSkipped = true,
                        Message = $"Reservation status is no longer Reserved (current status: {reservation.Status})."
                    };
                }

                if (reservation.ExpireAt > Clock.Now)
                {
                    return new ExpireReservationResult
                    {
                        ReservationId = id,
                        IsSkipped = true,
                        Message = "Reservation has not yet reached expiration deadline."
                    };
                }

                await ExpireAsync(reservation);

                await uow.CompleteAsync();

                return new ExpireReservationResult
                {
                    ReservationId = id,
                    IsSuccess = true,
                    Message = "Reservation successfully expired and resources released."
                };
            }
        }
        catch (BusinessRuleException ex) when (ex.Message.Contains("Only reserved reservations can be expired"))
        {
            Logger.Warn($"Reservation {id} was skipped during expiration: {ex.Message}");
            return new ExpireReservationResult
            {
                ReservationId = id,
                IsSkipped = true,
                Message = ex.Message
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"Error expiring reservation {id}: {ex.Message}", ex);
            return new ExpireReservationResult
            {
                ReservationId = id,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }

    public async Task ExpireAsync(Reservation reservation)
    {
        var dbContext = _repository.GetDbContext();
        var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Reservations
            SET Status = {ReservationStatus.Expired},
                LastModificationTime = GETUTCDATE()
            WHERE Id = {reservation.Id}
              AND Status = {ReservationStatus.Reserved}
              AND ExpireAt <= GETUTCDATE()  
              AND IsDeleted = 0
        ");

        if (rowsAffected == 0)
            throw new BusinessRuleException("Only reserved reservations can be expired.");
        await ReleaseReservationResourceAsync(reservation);
    }

    public async Task CancelAsync(Reservation reservation)
    {
        var dbContext = _repository.GetDbContext();
        var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Reservations
            SET Status = {ReservationStatus.Cancelled}
            WHERE Id = {reservation.Id}
              AND Status = {ReservationStatus.Reserved}
              AND ExpireAt > GETUTCDATE()   
              AND IsDeleted = 0
        ");

        if (rowsAffected == 0)
            throw new BusinessRuleException("Only reserved reservations can be cancelled.");

       
        await ReleaseReservationResourceAsync(reservation);
    }
    public async Task InvalidateAsync(Reservation reservation)
    {
        var dbContext = _repository.GetDbContext();
        var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Reservations
            SET Status = {ReservationStatus.Invalid}
            WHERE Id = {reservation.Id}
              AND Status = {ReservationStatus.Reserved}
              AND ExpireAt > GETUTCDATE()  
              AND IsDeleted = 0
        ");

        if (rowsAffected == 0)
            throw new BusinessRuleException("Only reserved reservations can be invalid.");

        await ReleaseReservationResourceAsync(reservation);
    }

    public async Task CompleteAsync(Reservation reservation)
    {
        var dbContext = _repository.GetDbContext();
        var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Reservations
            SET Status = {ReservationStatus.Completed}
            WHERE Id = {reservation.Id}
              AND Status = {ReservationStatus.Reserved}
              AND ExpireAt > GETUTCDATE()  
              AND IsDeleted = 0
        ");

        if (rowsAffected == 0)
            throw new BusinessRuleException("Reservation is no longer valid for check-in.");
    }

    private async Task ReleaseReservationResourceAsync(Reservation reservation)
    {
        if (reservation.ParkingSpotId.HasValue)
        {
            var spotDbContext = _parkingSpotRepository.GetDbContext();
            // Chỉ reset về Available nếu spot vẫn đang Reserved (tránh ghi đè Occupied)
            await spotDbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ParkingSpots
            SET Status = {ParkingSpotStatus.Available}
            WHERE Id = {reservation.ParkingSpotId.Value}
            AND ParkingAreaId = {reservation.ParkingAreaId} 
            AND Status = {ParkingSpotStatus.Reserved}
            AND IsDeleted = 0
        ");
           
        }
        var parkingArea = reservation.ParkingArea
            ?? await _parkingAreaRepository.FirstOrDefaultAsync(reservation.ParkingAreaId);

        if (parkingArea == null)
            throw new BusinessRuleException("Parking area not found for reservation: " + reservation.Id);

        if (parkingArea.ParkingMode == ParkingMode.capacityBase)
        {
            var dbContext = _parkingAreaRepository.GetDbContext();
            var areaRowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ParkingAreas
            SET CurrentOccupancy = CurrentOccupancy - 1
            WHERE Id = {reservation.ParkingAreaId}
              AND CurrentOccupancy > 0
              AND IsDeleted = 0
        ");
            if (areaRowsAffected == 0)
                throw new BusinessRuleException("This parking area is empty");
        }
    }
}
