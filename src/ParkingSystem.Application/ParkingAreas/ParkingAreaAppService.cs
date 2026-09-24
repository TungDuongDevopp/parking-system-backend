

using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Collections.Extensions;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using ParkingSystem.Helpers;
using ParkingSystem.ParkingAreas.Dto;
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
              nameof(ParkingArea.VehicleType)
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
            x => x.Capacity <= input.MaxCapacity.Value);
    }

    [AbpAuthorize(PermissionNames.Pages_ParkingAreas_Manager)]
    public override async Task<ParkingAreaDto> CreateAsync(CreateParkingAreaDto input)
    {
        if (await Repository.GetAll().AnyAsync(x => x.ParkingCode == input.ParkingCode))
        {
            throw new DuplicateResourceException("Parking area code already exists.");
        }
        var entity = ObjectMapper.Map<ParkingArea>(input);

        var created = await Repository.InsertAsync(entity);

        await CurrentUnitOfWork.SaveChangesAsync();

        return MapToEntityDto(created);
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

        await Repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();
        return MapToEntityDto(entity);
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
        var entity = await Repository.FirstOrDefaultAsync(input.Id);
        if (entity == null)
        {
            throw new ResourceNotFoundException("Parking area not found with id: " + input.Id);
        }

        entity.Status = input.Status;
        await Repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();
        return MapToEntityDto(entity);
    }


}
