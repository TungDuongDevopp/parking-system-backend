using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using System.Threading.Tasks;

namespace ParkingSystem.ParkingSessions;

public class SessionManager : DomainService
{
    private readonly IRepository<ParkingSession, long> _repository;
    private readonly IRepository<ParkingArea, long> _parkingAreaRepository;
    private readonly IRepository<ParkingSpot, long> _parkingSpotRepository;

    public SessionManager(
        IRepository<ParkingSession, long> repository,
        IRepository<ParkingArea, long> parkingAreaRepository,
        IRepository<ParkingSpot, long> parkingSpotRepository)
    {
        _repository = repository;
        _parkingAreaRepository = parkingAreaRepository;
        _parkingSpotRepository = parkingSpotRepository;
    }

    public async Task ReleaseSessionResourceAsync(ParkingSession session)
    {
        if (session.ParkingSpotId.HasValue)
        {
            var spotDbContext = _parkingSpotRepository.GetDbContext();
            var spotRowsAffected = await spotDbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ParkingSpots
            SET Status = {ParkingSpotStatus.Available}
            WHERE Id = {session.ParkingSpotId.Value}
              AND Status = {ParkingSpotStatus.Occupied}
              AND ParkingAreaId = {session.ParkingAreaId} 
              AND IsDeleted = 0
            ");
            if (spotRowsAffected == 0)
                throw new BusinessRuleException("The selected parking spot is not available.");
        }

        var parkingArea = session.ParkingArea
            ?? await _parkingAreaRepository.FirstOrDefaultAsync(session.ParkingAreaId);

        if (parkingArea == null)
            throw new BusinessRuleException("Parking area not found for session: " + session.Id);

        if (parkingArea.ParkingMode == ParkingMode.capacityBase)
        {
            var dbContext = _parkingAreaRepository.GetDbContext();
            var areaRowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ParkingAreas
            SET CurrentOccupancy = CurrentOccupancy - 1
            WHERE Id = {session.ParkingAreaId}
              AND CurrentOccupancy > 0
              AND IsDeleted = 0
            ");
            if (areaRowsAffected == 0)
                throw new BusinessRuleException("This parking area is empty");
        }
    }

    public async Task CompleteAsync(ParkingSession session, long staffId, decimal fee)
    {
        var dbContext = _repository.GetDbContext();

        var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
        UPDATE ParkingSessions
        SET Status = {ParkingSessionStatus.Paid},
            ExitTime = GETUTCDATE(),
            CheckOutStaffId = {staffId},
            Fee = {fee},
            LastModificationTime = GETUTCDATE()
        WHERE Id = {session.Id}
          AND (Status = {ParkingSessionStatus.Active} OR Status = {ParkingSessionStatus.PendingPayment})
        ");

        if (rowsAffected == 0)
            throw new BusinessRuleException(
                "Session is no longer valid for checkout.");

        await ReleaseSessionResourceAsync(session);
    }

    public async Task CompleteFromPaymentAsync(ParkingSession session)
    {
        var dbContext = _repository.GetDbContext();

        var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
        UPDATE ParkingSessions
        SET Status = {ParkingSessionStatus.Paid},
            ExitTime = GETUTCDATE(),
            LastModificationTime = GETUTCDATE()
        WHERE Id = {session.Id}
          AND (Status = {ParkingSessionStatus.PendingPayment} OR Status = {ParkingSessionStatus.Active})
        ");

        if (rowsAffected == 0)
            throw new BusinessRuleException(
                "Không thể hoàn tất phiên đỗ xe: phiên không tồn tại hoặc không ở trạng thái hợp lệ.");

        await ReleaseSessionResourceAsync(session);
    }
}
