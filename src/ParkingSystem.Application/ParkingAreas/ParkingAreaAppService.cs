using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Collections.Extensions;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using ParkingSystem.Helpers;
using ParkingSystem.ParkingAreas.Dto;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace ParkingSystem.ParkingAreas;

[AbpAuthorize(PermissionNames.Pages_ParkingAreas)]
public class ParkingAreaAppService: AsyncCrudAppService<ParkingArea,ParkingAreaDto,long,PagedParkingAreaResultRequestDto,CreateParkingAreaDto,UpdateParkingAreaDto>,IParkingAreaAppService
{
    private readonly IRepository<ParkingSpot, long> _parkingSpotRepository;

    public ParkingAreaAppService(IRepository<ParkingArea,long> repository, IRepository<ParkingSpot, long> parkingSpotRepository) : base(repository) {
        _parkingSpotRepository = parkingSpotRepository;
    }
    private void EnsureAreaConfigurable(ParkingArea area)
    {
        if (area.Status == ParkingAreaStatus.Active)
        {
            throw new CannotManipulateException(
                "Cannot modify parking area configuration while it is active.");
        }
    }


    protected override IQueryable<ParkingArea> ApplySorting(IQueryable<ParkingArea> query, PagedParkingAreaResultRequestDto input)
    {
        if (!string.IsNullOrEmpty(input.Sorting))
        {
            var sorting = SortingHelper.ValidateSorting(
              input.Sorting,
              nameof(ParkingArea.Id),
              nameof(ParkingArea.ParkingCode),
              nameof(ParkingArea.Name),
              nameof(ParkingArea.Location),
              nameof(ParkingArea.CreationTime),
              nameof(ParkingArea.Status),
              nameof(ParkingArea.Capacity),
              nameof(ParkingArea.ParkingMode),
              nameof(ParkingArea.VehicleType),
              nameof(ParkingArea.CurrentOccupancy)
               );

            return query.OrderBy(sorting);
        }
        return query.OrderByDescending(x => x.Id);
    }
    protected override IQueryable<ParkingArea> CreateFilteredQuery(PagedParkingAreaResultRequestDto input)
    {
        var query = Repository.GetAll().AsNoTracking();

        return query
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
               x.Name.Contains(input.Keyword) || 
               x.ParkingCode.Contains(input.Keyword) ||
               x.Location.Contains(input.Keyword) ||
               x.Description.Contains(input.Keyword))

        .WhereIf(input.VehicleType.HasValue,
            x => x.VehicleType == input.VehicleType.Value)
        .WhereIf(input.ParkingMode.HasValue,
            x => x.ParkingMode == input.ParkingMode.Value)
        .WhereIf(input.MinCapacity.HasValue,
            x=>x.Capacity >=input.MinCapacity.Value)
        .WhereIf(input.MaxCapacity.HasValue,
            x => x.Capacity <= input.MaxCapacity.Value)
        .WhereIf(input.Status.HasValue, x=> x.Status == input.Status.Value);
    }

    [AbpAuthorize(PermissionNames.Pages_ParkingAreas_Manager)]
    public override async Task<ParkingAreaDto> CreateAsync(CreateParkingAreaDto input)
    {
        if (await Repository.GetAll().AnyAsync(x => x.ParkingCode == input.ParkingCode))
        {
            throw new DuplicateResourceException("Parking area code already exists.");
        }
        var entity = ObjectMapper.Map<ParkingArea>(input);

        try
        {
            var created = await Repository.InsertAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();
            return MapToEntityDto(created);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            var errorMessage = sqlEx.Message;
            if (errorMessage.Contains("ParkingCode", StringComparison.OrdinalIgnoreCase))
            {
                throw new DuplicateResourceException("Parking area code already exists.");
            }
            throw new DuplicateResourceException("A parking area with conflicting data already exists.");
        }
    }
    [AbpAuthorize(PermissionNames.Pages_ParkingAreas_Manager)]
    public override async Task<ParkingAreaDto> UpdateAsync(UpdateParkingAreaDto input)
    {
        var entity = await Repository.FirstOrDefaultAsync(input.Id);
        if(entity == null)
        {
            throw new ResourceNotFoundException("Parking area not found with id: "+input.Id);
        }
        if(!string.IsNullOrWhiteSpace(input.ParkingCode) && input.ParkingCode != entity.ParkingCode)
{
            var existAreaCode = await Repository.GetAll()
                .AnyAsync(x =>
                    x.Id != input.Id &&
                    x.ParkingCode == input.ParkingCode);

            if (existAreaCode)
            {
                throw new DuplicateResourceException(
                    "Parking area code already exists.");
            }
        }
        if (input.Capacity.HasValue || input.ParkingMode.HasValue || input.VehicleType.HasValue)
        {
            EnsureAreaConfigurable(entity);
        }
        
        ObjectMapper.Map(input, entity);

        try
        {
            await Repository.UpdateAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();
            return MapToEntityDto(entity);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            var errorMessage = sqlEx.Message;
            if (errorMessage.Contains("ParkingCode", StringComparison.OrdinalIgnoreCase))
            {
                throw new DuplicateResourceException("Parking area code already exists.");
            }
            throw new DuplicateResourceException("A parking area with conflicting data already exists.");
        }
    }

    public override async Task<ParkingAreaDto> GetAsync(EntityDto<long> input)
    {
        var parkingArea = await Repository.FirstOrDefaultAsync(x => x.Id == input.Id);

        if (parkingArea == null)
        {
            throw new ResourceNotFoundException("Parking Area not found with id: " + input.Id);
        }

        return ObjectMapper.Map<ParkingAreaDto>(parkingArea);
    }

    [AbpAuthorize(PermissionNames.Pages_ParkingAreas_Manager)]
    public override async Task DeleteAsync(EntityDto<long> input)
    {
        var entity = await Repository.FirstOrDefaultAsync(input.Id);
        if (entity == null)
        {
            throw new ResourceNotFoundException("Parking Area not found with id: " + input.Id);
        }
        EnsureAreaConfigurable(entity);

        var hasSpots = await _parkingSpotRepository
        .GetAll()
        .AnyAsync(x => x.ParkingAreaId == input.Id);

        if (hasSpots)
        {
            throw new CannotManipulateException(
                "Cannot delete parking area because it still contains parking spots.");
        }
        await Repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

    }
    [AbpAuthorize(PermissionNames.Pages_ParkingAreas_Manager)]
    public async Task<ParkingAreaDto> ChangeStatus(ChangeStatusDto input)
    {
        var area = await Repository.FirstOrDefaultAsync(input.Id);
        if (area == null)
        {
            throw new ResourceNotFoundException("Parking area not found with id: " + input.Id);
        }

        if(area.CurrentOccupancy > 0)
        {
            throw new BusinessRuleException("Cannot deactivate because it is currently using.");
        }
        
        if(area.ParkingMode == ParkingMode.individualSpot)
        {
            var hasSpotActive =  await _parkingSpotRepository.GetAll().AnyAsync(x=>x.ParkingAreaId == area.Id && (x.Status == ParkingSpotStatus.Occupied || x.Status == ParkingSpotStatus.Reserved)); 
            if(hasSpotActive)
            {
                throw new BusinessRuleException("Cannot deactivate because another service is currently using it.");
            }
        }

        area.Status = input.Status;
        await Repository.UpdateAsync(area);
        await CurrentUnitOfWork.SaveChangesAsync();
        return MapToEntityDto(area);
    }
}
