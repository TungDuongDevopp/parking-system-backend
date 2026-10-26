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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace ParkingSystem.ParkingAreas;

[AbpAuthorize(PermissionNames.Pages_ParkingAreas, PermissionNames.Pages_ParkingAreas_Lookup)]
public class ParkingAreaAppService: AsyncCrudAppService<ParkingArea,ParkingAreaDto,long,PagedParkingAreaResultRequestDto,CreateParkingAreaDto,UpdateParkingAreaDto>,IParkingAreaAppService
{
    private readonly IRepository<ParkingSpot, long> _parkingSpotRepository;

    public ParkingAreaAppService(IRepository<ParkingArea,long> repository, IRepository<ParkingSpot, long> parkingSpotRepository) : base(repository) {
        _parkingSpotRepository = parkingSpotRepository;
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
            throw new ResourceNotFoundException("Parking area not found with id: " + input.Id);
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
        if (entity.Status == ParkingAreaStatus.Active)
        {
            if (input.Capacity.HasValue && input.Capacity.Value != entity.Capacity)
            {
                throw new BusinessRuleException(
                    "Cannot change capacity while the parking area is active.");
            }

            if (input.ParkingMode.HasValue && input.ParkingMode.Value != entity.ParkingMode)
            {
                throw new BusinessRuleException(
                    "Cannot change parking mode while the parking area is active.");
            }

            if (input.VehicleType.HasValue && input.VehicleType.Value != entity.VehicleType)
            {
                throw new BusinessRuleException(
                    "Cannot change vehicle type while the parking area is active.");
            }
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
        if(entity.Status == ParkingAreaStatus.Active)
        {
            throw new BusinessRuleException("Can delete while the parking area is active.");
        }

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

    [AbpAuthorize(PermissionNames.Pages_ParkingAreas_Lookup, PermissionNames.Pages_ParkingAreas)]
    public async Task<List<ParkingAreaLookupDto>> GetAvailableAreasAsync()
    {
        var activeAreas = await Repository.GetAll()
            .AsNoTracking()
            .Where(x => x.Status == ParkingAreaStatus.Active)
            .OrderBy(a => a.ParkingCode)
            .ToListAsync();

        if (activeAreas.Count == 0)
        {
            return new List<ParkingAreaLookupDto>();
        }

        var individualAreaIds = activeAreas
            .Where(a => a.ParkingMode == ParkingMode.individualSpot)
            .Select(a => a.Id)
            .ToList();

        var spotStatusByArea = individualAreaIds.Count > 0
            ? await _parkingSpotRepository.GetAll()
                .AsNoTracking()
                .Where(s => individualAreaIds.Contains(s.ParkingAreaId))
                .GroupBy(s => new { s.ParkingAreaId, s.Status })
                .Select(g => new { g.Key.ParkingAreaId, g.Key.Status, Count = g.Count() })
                .ToListAsync()
            : null;

        var result = new List<ParkingAreaLookupDto>();

        foreach (var area in activeAreas)
        {
            int unavailable;
            if (area.ParkingMode == ParkingMode.capacityBase)
            {
                unavailable = Math.Clamp(area.CurrentOccupancy, 0, area.Capacity);
            }
            else
            {
                var spots = spotStatusByArea?.Where(s => s.ParkingAreaId == area.Id).ToList();
                var occupiedSpots = spots?.FirstOrDefault(s => s.Status == ParkingSpotStatus.Occupied)?.Count ?? 0;
                var reservedSpots = spots?.FirstOrDefault(s => s.Status == ParkingSpotStatus.Reserved)?.Count ?? 0;
                var unavailableSpots = spots?.FirstOrDefault(s => s.Status == ParkingSpotStatus.Unavailable)?.Count ?? 0;
                unavailable = Math.Min(area.Capacity, occupiedSpots + reservedSpots + unavailableSpots);
            }

            var available = Math.Max(0, area.Capacity - unavailable);

            result.Add(new ParkingAreaLookupDto
            {
                Id = area.Id,
                ParkingCode = area.ParkingCode,
                Name = area.Name,
                Location = area.Location,
                VehicleType = area.VehicleType,
                ParkingMode = area.ParkingMode,
                Status = area.Status,
                Capacity = area.Capacity,
                AvailableCapacity = available,
                UnavailableCapacity = unavailable,
                Description = area.Description
            });
        }

        return result;
    }
}
