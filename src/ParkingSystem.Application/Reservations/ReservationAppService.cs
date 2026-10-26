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
    private readonly ReservationManager _manager;
    private readonly IRepository<Subscription, long> _subscriptionRepository;
    private const int ReservationDurationHours = 3;
    private const int ReservationGracePeriodMinutes = 15;
    


    public ReservationAppService(IRepository<Customer, long> customerRepository, IRepository<ParkingArea, long> parkingAreaRepository,
        IRepository<ParkingSpot, long> parkingSpotRepository, IRepository<Reservation, long> repository,
        IRepository<Subscription, long> subscriptionRepository, ReservationManager manager
        )
    {
        _customerRepository = customerRepository;
        _parkingAreaRepository = parkingAreaRepository;
        _parkingSpotRepository = parkingSpotRepository;
        _repository = repository;
        _subscriptionRepository = subscriptionRepository;
        _manager = manager;
    }
    private static readonly TimeZoneInfo LocalTimeZone = GetLocalTimeZone();
    private static TimeZoneInfo GetLocalTimeZone()
    {
        // Hỗ trợ cả tên Windows và IANA (Linux/Docker)
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
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

        var phoneNumber = customer.PhoneNumber == null ? throw new BusinessRuleException("Please provide your phone by updating your profile") : customer.PhoneNumber;

        // 3. Check active subscription
        var subscription = await _subscriptionRepository
            .FirstOrDefaultAsync(x =>
                x.CustomerId == customer.Id &&
                x.Status == SubscriptionStatus.inUse);

        if (subscription == null)
        {
            var hasPending = await _subscriptionRepository
                .GetAll()
                .AnyAsync(x =>
                    x.CustomerId == customer.Id &&
                    x.Status == SubscriptionStatus.pending);

            if (hasPending)
            {
                throw new BusinessRuleException(
                    "Your subscription is pending payment. Please complete the payment before making a reservation.");
            }

            throw new BusinessRuleException(
                "You need an active subscription to make a reservation.");
        }
        // 4. Check active/ expried reservation

        var expiredReservation =
        await _repository.FirstOrDefaultAsync(x =>
            x.CustomerId == customer.Id &&
            x.Status == ReservationStatus.Reserved &&
            x.ExpireAt <= Clock.Now);

        if (expiredReservation != null)
        {
            await _manager.ExpireAsync(expiredReservation);
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

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(Clock.Now, LocalTimeZone);
        var localReservedAt = TimeZoneInfo.ConvertTimeFromUtc(input.ReservedAt, LocalTimeZone);
        if (localReservedAt.Date != localNow.Date)
        {
            throw new BusinessRuleException("Reservation must be made for the current day.");
        }

       
        if (input.ReservedAt < Clock.Now.AddMinutes(-5))
        {
            throw new BusinessRuleException("Reserved time cannot be in the past.");
        }

        // 6. Get active parking area and still capa available
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

            // Atomic update: chỉ reserve nếu spot vẫn còn Available
            var spotDbContext = _parkingSpotRepository.GetDbContext();
            var spotRowsAffected = await spotDbContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ParkingSpots
                SET Status = {ParkingSpotStatus.Reserved}
                WHERE Id = {input.ParkingSpotId.Value}
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

        // 8. Create reservation
        var reservation = ObjectMapper.Map<Reservation>(input);
        reservation.CustomerId = customer.Id;
        reservation.ExpireAt = input.ReservedAt.AddHours(ReservationDurationHours).AddMinutes(ReservationGracePeriodMinutes);
        reservation.ContactPhone = phoneNumber;

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
        var keyword = input.Keyword?.Trim();
        var customerName = input.CustomerName?.Trim();
        var parkingAreaCode = input.ParkingAreaCode?.Trim();
        var parkingSpotCode = input.ParkingSpotCode?.Trim();

        query = query
            .WhereIf(
                !string.IsNullOrWhiteSpace(keyword),
                x => (x.Customer != null && x.Customer.Name.Contains(keyword))
                    || (x.ParkingArea != null && x.ParkingArea.ParkingCode.Contains(keyword))
                    || (x.ParkingSpot != null && x.ParkingSpot.SpotCode.Contains(keyword))
                    || x.ContactPhone.Contains(keyword)
            )
            .WhereIf(
                !string.IsNullOrWhiteSpace(customerName),
                x => x.Customer != null && x.Customer.Name.Contains(customerName)
            )
            .WhereIf(
                !string.IsNullOrWhiteSpace(parkingAreaCode),
                x => x.ParkingArea != null && x.ParkingArea.ParkingCode.Contains(parkingAreaCode)
            )
            .WhereIf(
                !string.IsNullOrWhiteSpace(parkingSpotCode),
                x => x.ParkingSpot != null && x.ParkingSpot.SpotCode.Contains(parkingSpotCode)
            )
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
                input.ReservedAtFrom.HasValue,
                x => x.ReservedAt >= input.ReservedAtFrom)
            .WhereIf(
                input.ReservedAtTo.HasValue,
                x => x.ReservedAt <= input.ReservedAtTo)
            .WhereIf(
                input.ExpireAtFrom.HasValue,
                x => x.ExpireAt >= input.ExpireAtFrom)
            .WhereIf(
                input.ExpireAtTo.HasValue,
                x => x.ExpireAt <= input.ExpireAtTo)
            .WhereIf(
                input.Status.HasValue,
                x=> x.Status == input.Status
            );

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
                nameof(Reservation.ReservedAt),
                "customerName",
                "parkingAreaCode",
                "parkingSpotCode"
                );

            switch (sorting?.ToLowerInvariant())
            {
                case "customername asc":
                    query = query.OrderBy(x => x.Customer.Name);
                    break;
                case "customername desc":
                    query = query.OrderByDescending(x => x.Customer.Name);
                    break;
                case "parkingareacode asc":
                    query = query.OrderBy(x => x.ParkingArea.ParkingCode);
                    break;
                case "parkingareacode desc":
                    query = query.OrderByDescending(x => x.ParkingArea.ParkingCode);
                    break;
                case "parkingspotcode asc":
                    query = query.OrderBy(x => x.ParkingSpot != null ? x.ParkingSpot.SpotCode : null);
                    break;
                case "parkingspotcode desc":
                    query = query.OrderByDescending(x => x.ParkingSpot != null ? x.ParkingSpot.SpotCode : null);
                    break;
                default:
                    query = query.OrderBy(sorting);
                    break;
            }
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

    [AbpAuthorize(PermissionNames.Pages_Reservations_ViewAll)]
    public async Task<ReservationLookUpDto> GetReservationLookUpAsync()
    {
         var subQuery = _repository.GetAll().AsNoTracking();
        var customerIds = subQuery.Select(s => s.CustomerId).Distinct();
        var parkingAreaIds = subQuery.Select(s => s.ParkingAreaId).Distinct();
        var parkingSpotIds = subQuery.Select(s => s.ParkingSpotId).Distinct();

        var customers = await _customerRepository.GetAll()
           .AsNoTracking()
           .Where(c => customerIds.Contains(c.Id))
           .OrderBy(c => c.Name)
           .Select(c => new ReservationCustomerLookUpDto
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
                .Select(a => new ReservationParkingAreaLookUpDto
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
                  .Select(p => new ReservationParkingSpotLookUpDto
                  {
                      Id = p.Id,
                      SpotCode = p.SpotCode,

                  })
                  .ToListAsync();
        return new ReservationLookUpDto
        {
            Customers = customers,
            ParkingAreas = parkingAreas,
            ParkingSpots= parkingSpots
        };
    }

    public async Task<ReservationDto> Canceled(EntityDto<long> input)
    {
        var reservation = await _repository.FirstOrDefaultAsync(input.Id);
        if (reservation == null)
        {
            throw new ResourceNotFoundException(
                "Reservation not found with id: " + input.Id);
        }

        await CheckReservationAccessAsync(reservation);

        await _manager.CancelAsync(reservation);

        var dbContext = _repository.GetDbContext();

        await dbContext.Entry(reservation).ReloadAsync();

        return ObjectMapper.Map<ReservationDto>(reservation);
    }
}
