using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using System.Threading.Tasks;

namespace ParkingSystem.Reservations;

public class ReservationManager : DomainService
{
    private readonly IRepository<Reservation, long> _repository;
    private readonly IRepository<ParkingArea, long> _parkingAreaRepository;
    private readonly IRepository<ParkingSpot, long> _parkingSpotRepository;

    public ReservationManager(
        IRepository<Reservation, long> repository,
        IRepository<ParkingArea, long> parkingAreaRepository,
        IRepository<ParkingSpot, long> parkingSpotRepository)
    {
        _repository = repository;
        _parkingAreaRepository = parkingAreaRepository;
        _parkingSpotRepository = parkingSpotRepository;
    }

    public async Task ExpireAsync(Reservation reservation)
    {
        var dbContext = _repository.GetDbContext();
        var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Reservations
            SET Status = {ReservationStatus.Expired}
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
            var spotRowsAffected =  await spotDbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ParkingSpots
            SET Status = {ParkingSpotStatus.Available}
            WHERE Id = {reservation.ParkingSpotId.Value}
            AND ParkingAreaId = {reservation.ParkingAreaId} 
            AND IsDeleted = 0
        ");
            if (spotRowsAffected == 0)
                throw new BusinessRuleException("The selected parking spot is not available.");
        }

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
