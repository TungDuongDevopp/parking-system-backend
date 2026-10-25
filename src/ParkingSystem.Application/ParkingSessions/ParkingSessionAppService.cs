
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Collections.Extensions;
using Abp.Domain.Repositories;
using Abp.EntityFrameworkCore.Repositories;
using Abp.Linq.Extensions;
using Abp.Timing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using ParkingSystem.Helpers;
using ParkingSystem.ParkingSessions.Dto;
using ParkingSystem.Reservations;
using ParkingSystem.Reservations.Dto;
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
    private readonly SessionManager _sessionManager;
    private readonly IRepository<Quotation, long> _quotationRepository;
    private readonly IRepository<SubscriptionAllowedPlate, long> _plateRepository;
    private readonly IRepository<Subscription, long> _subscriptionRepository;
    public ParkingSessionAppService(IRepository<ParkingSession, long> repository,
        IRepository<Reservation, long> reservationRepository,
        IRepository<ParkingArea, long> parkingAreaRepository,
        IRepository<ParkingSpot, long> parkingSpotRepository,
        IRepository<Customer, long> customerRepository,
        IRepository<Staff, long> staffRepository,
        ReservationManager reservationManager,
        IRepository<Quotation, long> quotationRepository,
        IRepository<Subscription, long> subscriptionRepository,
        SessionManager sessionManager,
        IRepository<SubscriptionAllowedPlate, long> plateRepository
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
        _plateRepository = plateRepository;
        _sessionManager = sessionManager;
        _subscriptionRepository = subscriptionRepository;       
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

    private static decimal CaculateFee(DateTime entryTime,DateTime exitTime, decimal halfDayRate, decimal dailyRate) {
       
        if (entryTime > exitTime)
        {
            throw new BusinessRuleException("The entry time must not exceed the exit time.");
        }
        TimeSpan duration = exitTime - entryTime;

        if (duration.TotalMinutes <= 15) return 0;

        int fullDays = (int)(duration.TotalHours / 24);
        double remainingHours = duration.TotalHours % 24;

        decimal total = fullDays * dailyRate;

        if (remainingHours > 12)
        {
            total += dailyRate;
        }
        else if (remainingHours > 0)
        {
            total += halfDayRate;
        }
        return total;

    }

    private async Task<bool> IsFreeWhenHasSub(
     string plateNumber,
     VehicleType type)
    {
        var now = Clock.Now;

        return await _plateRepository.GetAll()
            .AnyAsync(x =>
                x.PlateNumber == plateNumber &&
                x.Subscription.Status == SubscriptionStatus.inUse &&
                x.Subscription.StartTime <= now &&
                x.Subscription.EndTime >= now &&
                x.Subscription.Quotation.VehicleType == type &&
                !_repository.GetAll().Any(s =>
                    s.SubscriptionId == x.SubscriptionId &&
                    s.Status == ParkingSessionStatus.Active
                ));
    }

    public async Task<CheckInResultDto> CheckInAsync(ParkingSessionCheckInDto input)
    {
        var userId = AbpSession.UserId;
        var staff =  await _staffRepository.FirstOrDefaultAsync(x => x.UserId == userId && x.Status == StaffStatus.Active);
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

        var quotationHourly =  await _quotationRepository.FirstOrDefaultAsync(x => x.VehicleType == input.VehicleType && x.DurationUnit == DurationUnit.Hour && x.Duration == 12);
        var quotationDaily =  await _quotationRepository.FirstOrDefaultAsync(x => x.VehicleType == input.VehicleType && x.DurationUnit == DurationUnit.Day && x.Duration == 1);
        if (quotationHourly == null || quotationDaily == null)
        {
            throw new BusinessRuleException(
                "Hourly or daily quotation is not configured");
        }
        var hourlyRate = quotationHourly.Price;
        var dailyRate = quotationDaily.Price;
        var ticketCode = GenerateTicketCode();
        
        if (!input.PlateNumber.IsNullOrEmpty())
        {
            var plateNumber = PlateNumberNormalizer.Normalize(input.PlateNumber);
            session.PlateNumber = plateNumber;
            if (await IsFreeWhenHasSub(plateNumber, input.VehicleType))
            {
                session.Fee = 0;

                // Null-safe: plate và sub phải tồn tại vì IsFreeWhenHasSub đã xác nhận
                var plate = await _plateRepository.FirstOrDefaultAsync(x =>
                    x.PlateNumber == plateNumber &&
                    x.Subscription.Status == SubscriptionStatus.inUse);

                if (plate == null)
                    throw new BusinessRuleException("Subscription plate not found for: " + plateNumber);

                var sub = await _subscriptionRepository.FirstOrDefaultAsync(x => x.Id == plate.SubscriptionId);
                if (sub == null)
                    throw new BusinessRuleException("Subscription not found for plate: " + plateNumber);

                session.CustomerId = sub.CustomerId;
                session.SubscriptionId = sub.Id;
            }
        }
        session.CheckInStaff = staff;
        session.CheckInStaffId = staff.Id;
        session.TicketCode = ticketCode;
        session.HourlyRate = hourlyRate;
        try
        {
            var created = await _repository.InsertAsync(session);
            await CurrentUnitOfWork.SaveChangesAsync();

            var sessionDto = ObjectMapper.Map<CheckInResultDto>(created);
            sessionDto.Success = true;
            sessionDto.Message = "Check-in successful";

            return sessionDto;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            var errorMessage = sqlEx.Message;

            if (errorMessage.Contains("UX_ParkingSessions_ActivePlateNumber", StringComparison.OrdinalIgnoreCase)
                || errorMessage.Contains("IX_ParkingSessions_PlateNumber", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("This vehicle is already in an active parking session.");
            }

            if (errorMessage.Contains("UX_ParkingSessions_ActiveParkingSpot", StringComparison.OrdinalIgnoreCase)
                || errorMessage.Contains("IX_ParkingSessions_ParkingSpotId", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("The selected parking spot is already occupied by another vehicle.");
            }

            if (errorMessage.Contains("TicketCode", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("A ticket conflict occurred. Please try checking in again.");
            }

            throw new BusinessRuleException("A conflict occurred while checking in. Please try again.");
        }

    }

    private async Task<ParkingSession> CheckInForReservationAsync(long? id,VehicleType type)
    {
        var reservation = await _reservationRepository.FirstOrDefaultAsync(x => x.Id == id && x.Status == ReservationStatus.Reserved);
        if (reservation == null)
        {
            throw new BusinessRuleException("Reservation not found with id: " + id);
        }
        var parkingArea = await _parkingAreaRepository.FirstOrDefaultAsync(x => x.Id == reservation.ParkingAreaId && x.Status == ParkingAreaStatus.Active);
       
        if (parkingArea == null)
        {
            throw new BusinessRuleException("Parking area is currently inactive or not found.");
        }

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
        return new ParkingSession
        {
            ParkingAreaId = reservation.ParkingAreaId,
            ParkingArea = parkingArea,
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

    public async Task<CheckOutResultDto> CheckOutAsync(ParkingSessionCheckOutDto input)
    {
        var session = await _repository.GetAll()
            .Include(x => x.ParkingArea)
            .FirstOrDefaultAsync(x => x.Id == input.Id && x.Status == ParkingSessionStatus.Active);

        if (session == null)
        {
            throw new ResourceNotFoundException("Session not found with id: " + input.Id);
        }

        var userId = AbpSession.UserId;
        var staff = await _staffRepository.FirstOrDefaultAsync(x => x.UserId == userId && x.Status == StaffStatus.Active);

        if (staff == null)
        {
            throw new BusinessRuleException("You don't have permission to do this action");
        }

        if (session.Fee == null)
        {
            if (session.HourlyRate == null || session.DailyRate == null)
                throw new BusinessRuleException("Session is missing rate configuration and cannot be charged.");

            session.Fee = CaculateFee(session.EntryTime, Clock.Now, session.HourlyRate.Value, session.DailyRate.Value);
            // TODO: Gọi cổng thanh toán nếu cần
        }

        await _sessionManager.CompleteAsync(session, staff.Id, session.Fee.Value);
        var dbContext = _repository.GetDbContext();

        await dbContext.Entry(session).ReloadAsync();
        var sessionDto = ObjectMapper.Map<CheckOutResultDto>(session);
        sessionDto.Success = true;
        sessionDto.Message = "Checkout successfully!";
        return sessionDto;
    }
    public async Task<PagedResultDto<ParkingSessionDto>> GetAllAsync(PagedParkingSessionResultRequestDto input)
    {
        IQueryable<ParkingSession> query = _repository.GetAll()
             .AsNoTracking()
             .Include(x => x.ParkingArea)
             .Include(x => x.ParkingSpot)
             .Include(x=>x.Customer);

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
        query = query.WhereIf(!input.Keyword.IsNullOrEmpty(),
            x=> x.CustomerId!=null && x.Customer.Name.Contains(input.Keyword)
                || (x.ParkingSpotId != null  && x.ParkingSpot.SpotCode.Contains(input.Keyword))
                || (x.ParkingArea.ParkingCode.Contains(input.Keyword))
                || (x.CheckInStaffId!= null && x.CheckInStaff.Name.Contains(input.Keyword))
                || (x.CheckOutStaff!= null && x.CheckOutStaff.Name.Contains(input.Keyword)))
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
                input.EntryFrom.HasValue,
                x => x.EntryTime >= input.EntryFrom)
             .WhereIf(
                input.EntryTo.HasValue,
                x => x.EntryTime <= input.EntryTo)
            .WhereIf(
                input.ExitFrom.HasValue,
                x => x.ExitTime >= input.ExitFrom)
            .WhereIf(
                input.ExitTo.HasValue,
                x => x.ExitTime <= input.ExitTo)
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
           .Include(x => x.ParkingSpot)
           .Include(x=>x.Customer);

        var session = await query.FirstOrDefaultAsync(x => x.Id == input.Id);
        if (session == null)
        {
            throw new ResourceNotFoundException(
                "Session not found with id: " + input.Id);
        }
        await CheckSessionViewAccessAsync(session);
        return ObjectMapper.Map<ParkingSessionDto>(session);
    }

    public async Task<ParkingSessionLookUpDto> GetSessionLookUpAsync()
    {
        var subQuery = _repository.GetAll().AsNoTracking();
        var customerIds = subQuery.Select(s => s.CustomerId).Distinct();
        var parkingAreaIds = subQuery.Select(s => s.ParkingAreaId).Distinct();
        var parkingSpotIds = subQuery.Select(s => s.ParkingSpotId).Distinct();
        var staffIds = subQuery
            .Where(s => s.CheckInStaffId.HasValue)
            .Select(s => s.CheckInStaffId.Value)
            .Concat(
                subQuery
                    .Where(s => s.CheckOutStaffId.HasValue)
                    .Select(s => s.CheckOutStaffId.Value)
            )
            .Distinct();
        var customers = await _customerRepository.GetAll()
          .AsNoTracking()
          .Where(c => customerIds.Contains(c.Id))
          .OrderBy(c => c.Name)
          .Select(c => new SessionCustomerLookUpDto
          {
              Id = c.Id,
              Name = c.Name,
              PhoneNumber = c.PhoneNumber
          })
          .ToListAsync();
        var parkingAreas = await _parkingAreaRepository.GetAll()
        .AsNoTracking()
        .Where(a => parkingAreaIds.Contains(a.Id))
        .OrderBy(a => a.ParkingCode)
        .Select(a => new SessionAreaLookUpDto
        {
            Id = a.Id,
            ParkingCode = a.ParkingCode,
            VehicleType = a.VehicleType
        })
        .ToListAsync();

        var parkingSpots = await _parkingSpotRepository.GetAll()
          .AsNoTracking()
          .Where(p => parkingSpotIds.Contains(p.Id))
          .OrderBy(p => p.SpotCode)
          .Select(p => new SessionSpotLookUpDto
          {
              Id = p.Id,
              SpotCode = p.SpotCode,

          })
          .ToListAsync();
        var staffs = await _staffRepository.GetAll()
            .AsNoTracking()
            .Where(s=>staffIds.Contains(s.Id))
             .OrderBy(p => p.Name)
          .Select(p => new SessionStaffLookUpDto
          {
              Id = p.Id,
              UserId=p.UserId,
              Name = p.Name
          }).ToListAsync();
        return new ParkingSessionLookUpDto
        {
            Customers = customers,
            Staffs = staffs,
            ParkingAreas = parkingAreas,
            ParkingSpots = parkingSpots
        };
    }
}
