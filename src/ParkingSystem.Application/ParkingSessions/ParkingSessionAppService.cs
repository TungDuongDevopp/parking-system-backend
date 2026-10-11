

using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Collections.Extensions;
using Abp.Domain.Repositories;
using Abp.EntityFrameworkCore.Repositories;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using ParkingSystem.Helpers;
using ParkingSystem.ParkingSessions.Dto;
using ParkingSystem.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ParkingSystem.ParkingSessions;

[AbpAuthorize(PermissionNames.Pages_ParkingSessions)]
public class ParkingSessionAppService : ParkingSystemAppServiceBase, IParkingSessionAppService
{
    private readonly IRepository<ParkingSession, long> _repository;
    private readonly IRepository<Reservation, long> _reservationRepository;
    private readonly IRepository<ParkingArea, long> _parkingAreaRepository;
    private readonly IRepository<ParkingSpot, long> _parkingSpotRepository;
    private readonly IRepository<Customer, long> _customerRepository;
    private readonly IRepository<Staff, long> _staffRepository;
    private readonly ReservationManager _reservationManager;
    private readonly IRepository<Quotation, long> _quotationRepository;

    public ParkingSessionAppService(IRepository<ParkingSession, long> repository,
        IRepository<Reservation, long> reservationRepository,
        IRepository<ParkingArea, long> parkingAreaRepository,
        IRepository<ParkingSpot, long> parkingSpotRepository,
        IRepository<Customer, long> customerRepository,
        IRepository<Staff, long> staffRepository,
        ReservationManager reservationManager,
        IRepository<Quotation, long> quotationRepository
        )
      
    {
        _repository = repository;
        _reservationRepository = reservationRepository;
        _parkingAreaRepository = parkingAreaRepository;
        _parkingSpotRepository = parkingSpotRepository;
        _customerRepository = customerRepository;
        _staffRepository = staffRepository;
        _reservationManager = reservationManager;
        _quotationRepository = quotationRepository;
    }
    private async Task CheckSessionViewAccessAsync(ParkingSession session)
    {
        var canViewAll = await PermissionChecker.IsGrantedAsync(
            PermissionNames.Pages_ParkingSessions_ViewAll
        );

        if (canViewAll)
            return;

        var userId = AbpSession.UserId;

        var customer = await _customerRepository.FirstOrDefaultAsync(x => x.Id == session.CustomerId);
        if (customer == null || customer.UserId != userId)
        {
            throw new AbpAuthorizationException("You do not have permission to view this session.");
        }
    }

    private static string GenerateTicketCode()
    {
        return $"PK-{Convert.ToHexString(
            RandomNumberGenerator.GetBytes(12))}";

    }
    public async Task<CheckInResultDto> CheckInAsync(ParkingSessionCheckInDto input)
    {
        var userId = AbpSession.UserId;
        var staff =  await _staffRepository.FirstOrDefaultAsync(x => x.UserId == userId);
        if(staff  == null)
        {
            throw new BusinessRuleException("You don't have permision to do this action");
        }

        if (input.ReservationId.HasValue == input.ParkingAreaId.HasValue)
        {
            throw new BusinessRuleException(
                "Provide either ReservationId or ParkingAreaId.");
        }

        ParkingSession session = null;
      
        if (input.ReservationId.HasValue)
        {
            session = await CheckInForReservationAsync(input.ReservationId.Value, input.VehicleType);
            if(session == null)
            {
                return new CheckInResultDto
                {
                    Success = false,
                    Message = "Reservation invalidated due to vehicle type mismatch"
                };
            }
            
        }
        
        else if(input.ParkingAreaId != null)
        {
            session = await CheckInForNotReservationAsync(input.VehicleType, input.ParkingAreaId.Value, input.ParkingSpotId);
            if(session == null)
            {
                return new CheckInResultDto
                {
                    Success = false,
                    Message = "Checkin false due to vehicle type mismatch"
                };
            }
        }
        var quotationHourly =  await _quotationRepository.FirstOrDefaultAsync(x => x.VehicleType == input.VehicleType && x.DurationUnit == DurationUnit.Hour && x.Duration == 1);
        var quotationDaily =  await _quotationRepository.FirstOrDefaultAsync(x => x.VehicleType == input.VehicleType && x.DurationUnit == DurationUnit.Day && x.Duration == 1);
        var hourlyRate = quotationHourly.Price;
        var dailyRate = quotationDaily.Price;
        var ticketCode = GenerateTicketCode();
        if (!input.PlateNumber.IsNullOrEmpty())
        {
            session.PlateNumber = input.PlateNumber;
        }
        session.CheckInStaff = staff;
        session.CheckInStaffId = staff.Id;
        session.TicketCode = ticketCode;
        session.HourlyRate = hourlyRate;
        session.DailyRate = dailyRate;
        var created = await _repository.InsertAsync(session);

        await CurrentUnitOfWork.SaveChangesAsync();

        var sessionDto =  ObjectMapper.Map<CheckInResultDto>(created);
        sessionDto.Success = true;
        sessionDto.Message = "Check-in successful";

        return sessionDto;

    }

    private async Task<ParkingSession> CheckInForReservationAsync(long? id,VehicleType type)
    {
        var reservation = await _reservationRepository.FirstOrDefaultAsync(x => x.Id == id && x.Status == ReservationStatus.Reserved);
        if (reservation == null)
        {
            throw new BusinessRuleException("Reservation not found with id: " + id);
        }
        var parkingArea = await _parkingAreaRepository.FirstOrDefaultAsync(x => x.Id == reservation.ParkingAreaId);
        if (parkingArea.VehicleType != type)
        {
            await _reservationManager.InvalidateAsync(reservation);
            return null;
        }
        await _reservationManager.CompleteAsync(reservation);

        if (reservation.ParkingSpotId.HasValue)
        {
            // Atomic update: chỉ ocuppied nếu spot đã trong trạng thái reserver
            var spotDbContext = _parkingSpotRepository.GetDbContext();
            var spotRowsAffected = await spotDbContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ParkingSpots
                SET Status = {ParkingSpotStatus.Occupied}
                WHERE Id = {reservation.ParkingSpotId.Value}
                  AND ParkingAreaId = {parkingArea.Id}
                  AND Status = {ParkingSpotStatus.Reserved}
                  AND IsDeleted = 0
            ");
            if (spotRowsAffected == 0)
            {
                throw new BusinessRuleException(
                    "The selected parking spot is not available.");
            }
        }

        var areaDbContext = _parkingAreaRepository.GetDbContext();
        var rowsAffected = await areaDbContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ParkingAreas
                SET CurrentOccupancy = CurrentOccupancy + 1
                WHERE Id = {parkingArea.Id}
                  AND CurrentOccupancy < Capacity
                  AND IsDeleted = 0
            ");
        if (rowsAffected == 0)
        {
            throw new BusinessRuleException("This parking area is full.");
        }

        return new ParkingSession
        {
            ParkingAreaId = reservation.ParkingAreaId,
            ParkingSpotId = reservation.ParkingSpotId,
            CustomerId = reservation.CustomerId
        };
    }

    private async Task<ParkingSession> CheckInForNotReservationAsync(VehicleType type, long parkingAreaId, long? parkingSportId)
    {
        var parkingArea = await _parkingAreaRepository.FirstOrDefaultAsync(x => x.Id == parkingAreaId && x.Status == ParkingAreaStatus.Active);

        if (parkingArea == null)
        {
            throw new ResourceNotFoundException(
                "Parking Area not found with id: " + parkingAreaId);
        }
        if(parkingArea.VehicleType != type)
        {
            return null;
        }

        ParkingSpot parkingSpot = null;

        if (parkingArea.ParkingMode == ParkingMode.capacityBase)
        {
           
            if (parkingSportId.HasValue)
            {
                throw new BusinessRuleException(
                    "This parking area does not allow choosing individual spot.");
            }

            var dbContext = _parkingAreaRepository.GetDbContext();
            var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ParkingAreas
                SET CurrentOccupancy = CurrentOccupancy + 1
                WHERE Id = {parkingArea.Id}
                  AND CurrentOccupancy < Capacity
                  AND IsDeleted = 0
                  AND Status = {ParkingAreaStatus.Active}");
            if (rowsAffected == 0)
            {
                throw new BusinessRuleException("This parking area is full.");
            }
        }

        else if (parkingArea.ParkingMode == ParkingMode.individualSpot)
        {
            // Individual mode requires SpotId
            if (!parkingSportId.HasValue)
            {
                throw new BusinessRuleException(
                    "Please choose a parking spot.");
            }
            parkingSpot = await _parkingSpotRepository.FirstOrDefaultAsync(x => x.Id == parkingSportId && x.ParkingAreaId ==parkingArea.Id);
            if(parkingSpot == null)
            {
                throw new ResourceNotFoundException("parkingSpot not found with id: " + parkingSportId);
            }
            // Atomic update: chỉ occupied nếu spot vẫn còn Available
            var spotDbContext = _parkingSpotRepository.GetDbContext();
            var spotRowsAffected = await spotDbContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ParkingSpots
                SET Status = {ParkingSpotStatus.Occupied}
                WHERE Id = {parkingSportId.Value}
                  AND ParkingAreaId = {parkingArea.Id}
                  AND Status = {ParkingSpotStatus.Available}
                  AND IsDeleted = 0
            ");

            if (spotRowsAffected == 0)
            {
                throw new BusinessRuleException(
                    "The selected parking spot is not available.");
            }
           
           
        }
        return new ParkingSession { 
            ParkingArea = parkingArea,
            ParkingAreaId = parkingAreaId,
            ParkingSpot = parkingSpot,
            ParkingSpotId = parkingSportId
        };
    }

    public Task<ParkingSessionDto> CheckOutAsync(ParkingSessionCheckOutDto input)
    {
        throw new System.NotImplementedException();
    }

    public async Task<PagedResultDto<ParkingSessionDto>> GetAllAsync(PagedParkingSessionResultRequestDto input)
    {
        IQueryable<ParkingSession> query = _repository.GetAll()
             .AsNoTracking()
             .Include(x => x.ParkingArea)
             .Include(x => x.ParkingSpot);

        // Permission / Ownership
        var canViewAll = PermissionChecker.IsGranted(
            PermissionNames.Pages_ParkingSessions_ViewAll
            );
        if (!canViewAll)
        {
            var userId = AbpSession.UserId ?? throw new AbpAuthorizationException(
                    "User is not logged in.");
            query = query.Where(x => x.Customer.UserId == userId);
        }
        query = query
            .WhereIf(
            input.CustomerId.HasValue,
            x => x.CustomerId == input.CustomerId)
            .WhereIf(
            input.ParkingSpotId.HasValue,
            x => x.ParkingSpotId == input.ParkingSpotId)
            .WhereIf(
            input.ParkingAreaId.HasValue,
            x => x.ParkingAreaId == input.ParkingAreaId)
            .WhereIf(
            input.CheckInStaffId.HasValue,
            x => x.CheckInStaffId == input.CheckInStaffId)
            .WhereIf(
            input.CheckOutStaffId.HasValue,
            x => x.CheckOutStaffId == input.CheckOutStaffId)
             .WhereIf(
                input.FromTime.HasValue,
                x => x.EntryTime >= input.FromTime)
            .WhereIf(
                input.ToTime.HasValue,
                x => x.ExitTime <= input.ToTime)
            .WhereIf(
                input.Status.HasValue,
                x => x.Status == input.Status
            )
            .WhereIf(
                input.MinFee.HasValue,
                x => x.Fee >= input.MinFee
            )
            .WhereIf(
                input.MaxFee.HasValue,
                x => x.Fee <= input.MaxFee
            )
              .WhereIf(
                input.MinDailyRate.HasValue,
                x => x.DailyRate >= input.MinDailyRate
            )
            .WhereIf(
                input.MaxDailyRate.HasValue,
                x => x.DailyRate <= input.MaxDailyRate
            )
             .WhereIf(
                input.MinHourlyRate.HasValue,
                x => x.HourlyRate >= input.MinHourlyRate
            )
            .WhereIf(
                input.MaxHourlyRate.HasValue,
                x => x.HourlyRate <= input.MaxHourlyRate
            );

        // Total count
        var totalCount = await query.CountAsync();

        //Sorting
        if (!string.IsNullOrEmpty(input.Sorting))
        {
            var sorting = SortingHelper.ValidateSorting(
                input.Sorting,
                nameof(ParkingSession.CustomerId),
                nameof(ParkingSession.ParkingSpotId),
                nameof(ParkingSession.ParkingAreaId),
                nameof(ParkingSession.CreationTime),
                nameof(ParkingSession.ExitTime),
                nameof(ParkingSession.Status),
                nameof(ParkingSession.EntryTime),
                nameof(ParkingSession.CheckInStaffId),
                 nameof(ParkingSession.CheckOutStaffId),
                 nameof(ParkingSession.DailyRate),
                 nameof(ParkingSession.HourlyRate),
                 nameof(ParkingSession.ParkingArea),
                 nameof(ParkingSession.TicketCode),
                 nameof(ParkingSession.Fee)
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
        var result = ObjectMapper.Map<List<ParkingSessionDto>>(items);
        return new PagedResultDto<ParkingSessionDto>(totalCount, result);
    }

    public async Task<ParkingSessionDto> GetAsync(EntityDto<long> input)
    {
        IQueryable<ParkingSession> query = _repository.GetAll()
           .AsNoTracking()
           .Include(x => x.ParkingArea)
           .Include(x => x.ParkingSpot);

        var session = await query.FirstOrDefaultAsync(x => x.Id == input.Id);
        if (session == null)
        {
            throw new ResourceNotFoundException(
                "Session not found with id: " + input.Id);
        }
        await CheckSessionViewAccessAsync(session);
        return ObjectMapper.Map<ParkingSessionDto>(session);
    }
}
