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
using ParkingSystem.Reservations.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
namespace ParkingSystem.Reservations;

[AbpAuthorize(PermissionNames.Pages_Reservations)]
public class ReservationAppService : ParkingSystemAppServiceBase, IReservationAppService
{
    private readonly IRepository<Customer, long> _customerRepository;
    private readonly IRepository<ParkingArea, long> _parkingAreaRepository;

    private readonly IRepository<ParkingSpot, long> _parkingSpotRepository;

    private readonly IRepository<Reservation, long> _repository;

    private readonly IRepository<Subscription, long> _subscriptionRepository;
    private const int ReservationDurationHours = 3;
    private const int ReservationGracePeriodMinutes = 15;
    


    public ReservationAppService(IRepository<Customer, long> customerRepository, IRepository<ParkingArea, long> parkingAreaRepository,
        IRepository<ParkingSpot, long> parkingSpotRepository, IRepository<Reservation, long> repository,
        IRepository<Subscription, long> subscriptionRepository
        )
    {
        _customerRepository = customerRepository;
        _parkingAreaRepository = parkingAreaRepository;
        _parkingSpotRepository = parkingSpotRepository;
        _repository = repository;
        _subscriptionRepository = subscriptionRepository;
    }

    private async Task CheckReservationViewAccessAsync(Reservation reservation)
    {
        var canViewAll = await PermissionChecker.IsGrantedAsync(
            PermissionNames.Pages_Reservations_ViewAll
        );

        if (canViewAll)
            return;

        var userId = AbpSession.UserId;

        var customer = await _customerRepository.FirstOrDefaultAsync(x => x.Id == reservation.CustomerId);
        if (customer == null || customer.UserId != userId)
        {
            throw new AbpAuthorizationException("You do not have permission to view this reservation.");
        }
    }
    private async Task CheckReservationAccessAsync(Reservation reservation)
    {
        if (await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Reservations_ModifyAll))
        {
            return;
        }

        var canModifyOwn = await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Reservations_ModifyOwn);
        if (!canModifyOwn)
        {
            throw new AbpAuthorizationException("You do not have permission to modify this reservation.");
        }

        var userId = AbpSession.UserId
            ?? throw new AbpAuthorizationException("User is not logged in.");

        var customer = await _customerRepository
            .FirstOrDefaultAsync(x => x.Id == reservation.CustomerId);

        if (customer == null || customer.UserId != userId)
        {
            throw new AbpAuthorizationException(
                "You do not have permission to access this reservation."
            );
        }
    }
    private async Task ReleaseReservationResourceAsync(
    Reservation reservation)
    {
        if (reservation.ParkingSpotId.HasValue)
        {
            var spot = await _parkingSpotRepository
                .FirstOrDefaultAsync(reservation.ParkingSpotId.Value);

            if (spot != null)
            {
                spot.Status = ParkingSpotStatus.Available;
            }

            return;
        }
        var dbContext = _parkingAreaRepository.GetDbContext();
        await dbContext.Database
            .ExecuteSqlInterpolatedAsync($@"
            UPDATE ParkingAreas
            SET CurrentOccupancy = CurrentOccupancy - 1
            WHERE Id = {reservation.ParkingAreaId}
              AND CurrentOccupancy > 0
              AND IsDeleted = 0
        ");
    }
    public async Task<ReservationDto> Canceled(EntityDto<long> input)

    {
        var reservation = await _repository.FirstOrDefaultAsync(input.Id);

        if (reservation == null)
        {
            throw new ResourceNotFoundException("Reservation not found with id: "+ input.Id);
        }

        await CheckReservationAccessAsync(reservation);

        if (reservation.Status != ReservationStatus.Reserved)
        {
            throw new BusinessRuleException(
                "Only reserved reservations can be cancelled.");
        }

        reservation.Status = ReservationStatus.Cancelled;
        await ReleaseReservationResourceAsync(reservation);
        return ObjectMapper.Map<ReservationDto>(reservation);


    }
    private async Task ExpireReservationAsync(Reservation reservation)
    {
        if (reservation.Status != ReservationStatus.Reserved)
            return;

        if (reservation.ExpireAt > Clock.Now)
            return;

        reservation.Status = ReservationStatus.Expired;

        await ReleaseReservationResourceAsync(reservation);
    }

    public async Task<ReservationDto> CreateAsync(CreateReservationDto input)
    {
        // 1. Get current user
        var userId = AbpSession.UserId
            ?? throw new AbpAuthorizationException("User is not logged in.");

        // 2. Get customer
        var customer = await _customerRepository
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (customer == null)
        {
            throw new BusinessRuleException(
                "You do not have customer profile.");
        }

        // 3. Check active subscription
        var subscription = await _subscriptionRepository
            .FirstOrDefaultAsync(x =>
                x.CustomerId == customer.Id &&
                x.Status == SubscriptionStatus.inUse);

        if (subscription == null)
        {
            throw new BusinessRuleException(
                "You must buy subscription package to use this feature.");
        }
        // 4. Check active/ expried reservation

            var expiredReservation =
        await _repository.FirstOrDefaultAsync(x =>
            x.CustomerId == customer.Id &&
            x.Status == ReservationStatus.Reserved &&
            x.ExpireAt <= Clock.Now);

        if (expiredReservation != null)
        {
            expiredReservation.Status = ReservationStatus.Expired;

            await ReleaseReservationResourceAsync(expiredReservation);
        }

        var activeReservation = await _repository
            .FirstOrDefaultAsync(x =>
                x.CustomerId == customer.Id &&
                x.Status == ReservationStatus.Reserved &&
                x.ExpireAt > Clock.Now);

        if (activeReservation != null)
        {
            throw new BusinessRuleException(
                "You already have an active reservation.");
        }
        // 5. Check time Reserver (on day)
        if (input.ReservedAt.Date != Clock.Now.Date)
        {
            throw new BusinessRuleException(
                "Reservation must be made for the current day.");
        }

        // 6. Get active parking area
        var parkingArea = await _parkingAreaRepository
            .FirstOrDefaultAsync(x =>
                x.Id == input.ParkingAreaId &&
                x.Status == ParkingAreaStatus.Active);

        if (parkingArea == null)
        {
            throw new ResourceNotFoundException(
                "Parking Area not found with id: " + input.ParkingAreaId);
        }

        // 7. Validate reservation based on ParkingMode
        if (parkingArea.ParkingMode == ParkingMode.capacityBase)
        {
            // Capacity-based area does not allow choosing individual spot
            if (input.ParkingSpotId != null)
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
            ");
            if (rowsAffected == 0)
            {
                throw new BusinessRuleException("This parking area is full.");
            }
        }
        else if (parkingArea.ParkingMode == ParkingMode.individualSpot)
        {
            // Individual mode requires SpotId
            if (input.ParkingSpotId == null)
            {
                throw new BusinessRuleException(
                    "Please choose a parking spot.");
            }

            // Spot must belong to selected area and be available
            var spot = await _parkingSpotRepository
                .FirstOrDefaultAsync(x =>
                    x.Id == input.ParkingSpotId.Value &&
                    x.ParkingAreaId == parkingArea.Id &&
                    x.Status == ParkingSpotStatus.Available);

            if (spot == null)
            {
                throw new ResourceNotFoundException(
                    "Available parking spot not found with id: " +
                    input.ParkingSpotId);
            }

            // Reserve spot
            spot.Status = ParkingSpotStatus.Reserved;
        }

        // 8. Create reservation
        var reservation = ObjectMapper.Map<Reservation>(input);

        reservation.CustomerId = customer.Id;
        reservation.ExpireAt = input.ReservedAt.AddHours(ReservationDurationHours).AddMinutes(ReservationGracePeriodMinutes);

        try
        {
            await _repository.InsertAsync(reservation);
            await CurrentUnitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            var errorMessage = sqlEx.Message;

            if (errorMessage.Contains("UX_Reservations_ActiveCustomer", StringComparison.OrdinalIgnoreCase)
                || errorMessage.Contains("IX_Reservations_CustomerId", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("You already have an active reservation.");
            }

            if (errorMessage.Contains("UX_Reservations_ActiveParkingSpot", StringComparison.OrdinalIgnoreCase)
                || errorMessage.Contains("IX_Reservations_ParkingSpotId", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("The selected parking spot has just been reserved by another customer.");
            }

            throw new BusinessRuleException("A conflict occurred while creating the reservation. Please try again.");
        }
        // 9. Return result
        return ObjectMapper.Map<ReservationDto>(reservation);
    }
    public async Task<PagedResultDto<ReservationDto>> GetAllAsync(PagedReservationResultRequestDto input)
    {
        IQueryable<Reservation> query = _repository.GetAll()
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.ParkingArea)
            .Include(x => x.ParkingSpot);

        // Permission / Ownership
        var canViewAll = PermissionChecker.IsGranted(
            PermissionNames.Pages_Reservations_ViewAll
            );
        if (!canViewAll)
        {
            var userId = AbpSession.UserId  ?? throw new AbpAuthorizationException(
                    "User is not logged in.");
            query = query.Where(x => x.Customer.UserId == userId);
        }
        query = query
            .WhereIf(
            input.CustomerId.HasValue,
            x=>x.CustomerId==input.CustomerId)
            .WhereIf(
            input.ParkingSpotId.HasValue,
            x => x.ParkingSpotId == input.ParkingSpotId)
            .WhereIf(
            input.ParkingAreaId.HasValue,
            x => x.ParkingAreaId== input.ParkingAreaId)
             .WhereIf(
                input.ReservedAt.HasValue,
                x => x.ReservedAt >= input.ReservedAt)
            .WhereIf(
                input.ExpireAt.HasValue,
                x => x.ExpireAt <= input.ExpireAt);

            // Total count
           var totalCount = await query.CountAsync();

        //Sorting
        if (!string.IsNullOrEmpty(input.Sorting))
        {
            var sorting = SortingHelper.ValidateSorting(
                input.Sorting,
                nameof(Reservation.CustomerId),
                nameof(Reservation.ParkingSpotId),
                nameof(Reservation.ParkingAreaId),
                nameof(Reservation.CreationTime),
                nameof(Reservation.ExpireAt),
                nameof(Reservation.Status),
                nameof(Reservation.ReservedAt)
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
        var result = ObjectMapper.Map<List<ReservationDto>>(items);
        return new PagedResultDto<ReservationDto>(totalCount,result);
    }

    public async Task<ReservationDto> GetAsync(EntityDto<long> input)
    {
        IQueryable<Reservation> query = _repository.GetAll()
             .AsNoTracking()
             .Include(x => x.Customer)
             .Include(x => x.ParkingArea)
             .Include(x => x.ParkingSpot);

        var reservation = await query.FirstOrDefaultAsync(x => x.Id == input.Id);
        if(reservation == null)
        {
            throw new ResourceNotFoundException(
                "Reservation not found with id: " + input.Id);
        }
        await CheckReservationViewAccessAsync(reservation);
        return ObjectMapper.Map<ReservationDto>(reservation);

    }
}
