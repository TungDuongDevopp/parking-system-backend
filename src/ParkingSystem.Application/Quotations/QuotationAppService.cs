

using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Collections.Extensions;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using ParkingSystem.Helpers;
using ParkingSystem.Quotations.Dto;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;


namespace ParkingSystem.Quotations;

[AbpAuthorize(PermissionNames.Pages_Quotations)]
public class QuotationAppService: AsyncCrudAppService<Quotation,QuotationDto,long,PagedQuotationResultRequestDto,CreateQuotationDto,UpdateQuotationDto>, IQuotationAppService
{
    public QuotationAppService(IRepository<Quotation,long> repository) : base(repository)
    {

    }
    private void ValidateDuration (DurationUnit unit, int duration,VehicleType type)
    {
        if (unit == DurationUnit.Hour && duration > 24)
        {
            throw new BusinessRuleException("The number of hours cannot exceed 24.");
        }
        if(unit == DurationUnit.Week && duration > 4)
        {
            throw new BusinessRuleException("The number of weeks cannot exceed 4.");
        }
        if (unit == DurationUnit.Day && duration > 31)
        {
            throw new BusinessRuleException("The number of days cannot exceed 31.");
        }
        if (unit == DurationUnit.Month && duration > 12)
        {
            throw new BusinessRuleException("The number of months cannot exceed 12.");
        }
        if(unit == DurationUnit.Year && duration > 2)
        {
            throw new BusinessRuleException("The number of years cannot exceed 2.");
        }
        if (type == VehicleType.Bicycle && (unit > DurationUnit.Month || unit == DurationUnit.Month && duration > 1))
        {
            throw new BusinessRuleException("You cannot sign up for a plan longer than one month for a bicycle.");

        }

        return;

    }

    protected override IQueryable<Quotation> ApplySorting(IQueryable<Quotation> query, PagedQuotationResultRequestDto input)
    {
        if (!string.IsNullOrEmpty(input.Sorting))
        {
            var sorting = SortingHelper.ValidateSorting(
            input.Sorting,
            nameof(Quotation.Id),
            nameof(Quotation.VehicleType),
            nameof(Quotation.Duration),
            nameof(Quotation.Price),
            nameof(Quotation.CreationTime),
            nameof(Quotation.DurationUnit)
             );

            return query.OrderBy(sorting);
        }
        return query.OrderByDescending(x => x.Id);
    }
    protected override IQueryable<Quotation> CreateFilteredQuery(PagedQuotationResultRequestDto input)
    {
        var query = Repository.GetAll().AsNoTracking();

        return query
        .WhereIf(input.VehicleType.HasValue,
            x => x.VehicleType == input.VehicleType.Value)
        .WhereIf(input.Duration.HasValue,
            x => x.Duration == input.Duration.Value)
        .WhereIf(input.DurationUnit.HasValue,
            x => x.DurationUnit == input.DurationUnit.Value)
        .WhereIf(input.MinPrice.HasValue,
            x => x.Price >= input.MinPrice.Value)
        .WhereIf(input.MaxPrice.HasValue,
            x => x.Price <= input.MaxPrice.Value);
    }

    [AbpAuthorize(PermissionNames.Pages_Quotations_Manager)]
    public override async Task<QuotationDto> UpdateAsync(UpdateQuotationDto input)
    {
        var entity = await Repository.FirstOrDefaultAsync(input.Id);

        if(entity == null)
        {
            throw new ResourceNotFoundException("Quotation not found with id: " + input.Id);
        }
        var vehicleType = input.VehicleType ?? entity.VehicleType;
        var duration = input.Duration ?? entity.Duration;
        var durationUnit = input.DurationUnit ?? entity.DurationUnit;

        var exists = await Repository.GetAll().AnyAsync(
            x => x.Id != input.Id
              && x.Duration == duration
              && x.DurationUnit == durationUnit
              && x.VehicleType == vehicleType
        );
        if (exists)
        {
            throw new DuplicateResourceException("Quotation already exists.");
        }
      
        ValidateDuration(durationUnit, duration,vehicleType);
        
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
            throw new DuplicateResourceException("Quotation already exists.");
        }
    }


    [AbpAuthorize(PermissionNames.Pages_Quotations_Manager)]
    public override async Task<QuotationDto> CreateAsync(CreateQuotationDto input)
    {
        var exists = await Repository.GetAll().AnyAsync(
             x => x.Duration == input.Duration
            && x.DurationUnit == input.DurationUnit
            && x.VehicleType == input.VehicleType
 );

        if (exists)
        {
            throw new DuplicateResourceException("Quotation already exists.");
        }

        ValidateDuration(input.DurationUnit, input.Duration,input.VehicleType);
        var entity = ObjectMapper.Map<Quotation>(input);

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
            throw new DuplicateResourceException("Quotation already exists.");
        }
    }

    public override async Task<QuotationDto> GetAsync(EntityDto<long> input)
    {
        var quotation = await Repository.FirstOrDefaultAsync(x => x.Id == input.Id);

        if (quotation == null)
        {
            throw new ResourceNotFoundException("Quotation not found with id: " + input.Id);
        }

        return ObjectMapper.Map<QuotationDto>(quotation);
    }

    [AbpAuthorize(PermissionNames.Pages_Quotations_Manager)]
    public override async Task DeleteAsync(EntityDto<long> input)
    {
        var entity = await Repository.FirstOrDefaultAsync(input.Id);
        if (entity == null)
        {
            throw new ResourceNotFoundException("Quotation not found with id: " + input.Id);
        }

        await Repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

    }
}
