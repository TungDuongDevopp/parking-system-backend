using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.EntityFrameworkCore.Repositories;
using Abp.Linq.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using ParkingSystem.Helpers;
using ParkingSystem.SubscriptionPlate.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace ParkingSystem.SubscriptionPlate;

[AbpAuthorize(PermissionNames.Pages_Subscription_Plate)]

public class SubscriptionPlateAppService : ParkingSystemAppServiceBase, ISubscriptionPlateAppService
{
    private readonly IRepository<Subscription, long> _subscriptionRepository;
    private readonly IRepository<SubscriptionAllowedPlate, long> _repository;
    private readonly IRepository<Customer, long> _customerRepository;
    private readonly IRepository<Staff, long> _staffRepository;

    public SubscriptionPlateAppService(IRepository<Subscription, long> subscriptionRepository,
        IRepository<SubscriptionAllowedPlate, long> repository,
        IRepository<Customer, long> customerRepository,
        IRepository<Staff, long> staffRepository)
    {
        _repository = repository;
        _customerRepository = customerRepository;
        _staffRepository = staffRepository;
        _subscriptionRepository = subscriptionRepository;
    }
   
    private async Task CheckPlateViewAccessAsync(SubscriptionAllowedPlate plate)
    {
        var canViewAll = await PermissionChecker.IsGrantedAsync(
            PermissionNames.Pages_Subscription_Plate_ViewAll);

        if (canViewAll)
            return;

        var userId = AbpSession.UserId;

        var customer = await _customerRepository.FirstOrDefaultAsync(x => x.Id == plate.Subscription.CustomerId);
        if (customer == null || customer.UserId != userId)
        {
            throw new AbpAuthorizationException("You do not have permission to view this plate.");
        }
    }

    private async Task CheckPlateAddAccessAsync(Subscription subscription)
    {
        var canAdd = await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Subscription_Plate_Add);
        if (canAdd)
            return;
        throw new AbpAuthorizationException("You do not have permission to add plate.");
    }
    private async Task CheckPlateChangeAccessAsync(SubscriptionAllowedPlate plate)
    {
        var canChange = await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Subscription_Plate_Change);
        if (canChange)
            return;
        throw new AbpAuthorizationException("You do not have permission to change this plate");
    }

    public async Task<SubscriptionPlateDto> AddPlateAsync(CreatePlateDto input)
    {
        var subscription = await _subscriptionRepository.FirstOrDefaultAsync(x => x.Id == input.SubscriptionId && x.Status == SubscriptionStatus.inUse);

        if (subscription == null)
        {
            throw new ResourceNotFoundException("Subscription not found with id " + input.SubscriptionId);
        }
       await CheckPlateAddAccessAsync(subscription);

        var plateNormalize = PlateNumberNormalizer.Normalize(input.PlateNumber);

        var dbContext = _repository.GetDbContext();

        var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO SubscriptionAllowedPlate (SubscriptionId, PlateNumber,CreationTime)
            SELECT s.Id, {plateNormalize},GETUTCDATE()
            FROM Subscriptions s WITH (UPDLOCK, HOLDLOCK)
            WHERE s.Id = {subscription.Id}
              AND s.Status = {(int)SubscriptionStatus.inUse}
              AND NOT EXISTS (
                  SELECT 1
                  FROM SubscriptionAllowedPlate p
                  WHERE p.SubscriptionId = s.Id
                    AND p.PlateNumber = {plateNormalize}
              )
              AND (
                  SELECT COUNT(*)
                  FROM SubscriptionAllowedPlate p
                  WHERE p.SubscriptionId = s.Id
              ) < 3
        ");

        if (rowsAffected == 0)
            throw new BusinessRuleException("Can add plate");

        var plate = await _repository.FirstOrDefaultAsync(x => x.SubscriptionId == subscription.Id && x.PlateNumber == input.PlateNumber);
        return ObjectMapper.Map<SubscriptionPlateDto>(plate);
    }

    public async Task<SubscriptionPlateDto> ChangePlateAsync(ChangePlateDto input)
    {
        var plate = await _repository.FirstOrDefaultAsync(x => x.Id == input.Id);

        if (plate == null)
            throw new ResourceNotFoundException("Plate not found with id " + input.Id);

        await CheckPlateChangeAccessAsync(plate);

        if (plate.SubscriptionId != input.SubscriptionId)
            throw new BusinessRuleException("Plate does not belong to subscription");

        var plateNormalize = PlateNumberNormalizer.Normalize(input.PlateNumber);

        var subscription = await _subscriptionRepository.FirstOrDefaultAsync(
            x => x.Id == input.SubscriptionId &&
                 x.Status == SubscriptionStatus.inUse);

        if (subscription == null)
            throw new ResourceNotFoundException("Subscription not found with id " + input.SubscriptionId);

        if (subscription.HasReplacedPlate)
            throw new BusinessRuleException("You cannot change plate anymore");

        var dbContext = _repository.GetDbContext();

        // 1. Giành quyền đổi biển số
        var subRowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
        UPDATE Subscriptions
        SET HasReplacedPlate = 1
        WHERE Id = {subscription.Id}
          AND Status = {(int)SubscriptionStatus.inUse}
          AND HasReplacedPlate = 0
    ");

        if (subRowsAffected != 1)
            throw new BusinessRuleException("Plate has already been replaced");

        // 2. Đổi biển số
        var plateRowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
        UPDATE SubscriptionAllowedPlate
        SET PlateNumber = {plateNormalize}
        WHERE Id = {plate.Id}
          AND SubscriptionId = {subscription.Id}
          AND NOT EXISTS (
              SELECT 1
              FROM SubscriptionAllowedPlate
              WHERE SubscriptionId = {subscription.Id}
                AND PlateNumber = {plateNormalize}
                AND Id <> {plate.Id}
          )
    ");

        if (plateRowsAffected != 1)
            throw new BusinessRuleException("Plate number already exists or update failed");

        await dbContext.Entry(plate).ReloadAsync();

        return ObjectMapper.Map<SubscriptionPlateDto>(plate);
    }

    public async Task<SubscriptionPlateDto> CreateAsync(CreatePlateDto input)
    {
        var subscription = await _subscriptionRepository.FirstOrDefaultAsync(
              x => x.Id == input.SubscriptionId &&
          x.Status == SubscriptionStatus.pending);

        if (subscription == null)
        {
            throw new ResourceNotFoundException("Subscription not found with id: " + input.SubscriptionId);
        }
        var maxNumOfPlate = await _repository.CountAsync(x => x.SubscriptionId == input.SubscriptionId) >= 3;
        if (maxNumOfPlate)
        {
            throw new BusinessRuleException("Subscription cannot have more than 3 plates.");
        }
        var userId = AbpSession.UserId;

        var customer = await _customerRepository.FirstOrDefaultAsync(x => x.Id == subscription.CustomerId);

        if(customer == null || customer.UserId != userId)
        {
            throw new AbpAuthorizationException("You do not have permission to create this plate.");
        }

        var plateNormalize = PlateNumberNormalizer.Normalize(input.PlateNumber);
       
        var subPlate = new SubscriptionAllowedPlate
        {
            PlateNumber = plateNormalize,
            SubscriptionId = subscription.Id
        };
        try
        {
            await _repository.InsertAsync(subPlate);
            await CurrentUnitOfWork.SaveChangesAsync();
            return ObjectMapper.Map<SubscriptionPlateDto>(subPlate);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            throw new DuplicateResourceException("This plate number is already registered for this subscription.");
        }
    }

    public async Task<PagedResultDto<SubscriptionPlateDto>> GetAllAysnc(PagedSubscriptionPlateResultRequestDto input)
    {
        IQueryable<SubscriptionAllowedPlate> query =  _repository.GetAll()
            .AsNoTracking();
            

        // Permission / Ownership
        var canViewAll =
            PermissionChecker.IsGranted(
                PermissionNames.Pages_Subscription_Plate_ViewAll);

        if (!canViewAll)
        {
            var userId = AbpSession.UserId
                ?? throw new AbpAuthorizationException(
                    "User is not logged in.");

            query = query.Where(x => x.Subscription.Customer.UserId == userId);
        }

        // Filter
        query = query
            .WhereIf(
                input.SubscriptionId.HasValue,
                x => x.SubscriptionId == input.SubscriptionId);
            
        // Total count
        var totalCount = await query.CountAsync();

        // Sorting
        if (!string.IsNullOrEmpty(input.Sorting))
        {
            var sorting = SortingHelper.ValidateSorting(
                input.Sorting,
                nameof(SubscriptionAllowedPlate.Id),
                nameof(SubscriptionAllowedPlate.PlateNumber),
                nameof(SubscriptionAllowedPlate.SubscriptionId),
                nameof(SubscriptionAllowedPlate.CreationTime)
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

        // Mapping
        var result = ObjectMapper.Map<List<SubscriptionPlateDto>>(items);

        return new PagedResultDto<SubscriptionPlateDto>(
            totalCount,
            result);
    }
    public async Task<SubscriptionPlateDto> GetAsync(EntityDto<long> input)
    {
        IQueryable<SubscriptionAllowedPlate> query = _repository.GetAll()
         .AsNoTracking()
         .Include(x=>x.Subscription);

        var plate = await query
            .FirstOrDefaultAsync(s => s.Id == input.Id);

        if (plate == null)
        {
            throw new ResourceNotFoundException(
                "Plate not found with id: " + input.Id);
        }

        await CheckPlateViewAccessAsync(plate);

        return ObjectMapper.Map<SubscriptionPlateDto>(plate);
    }
}
