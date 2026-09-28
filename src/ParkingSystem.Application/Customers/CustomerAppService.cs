using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Customers.Dto;
using ParkingSystem.Entities;
using ParkingSystem.Exceptions;
using ParkingSystem.Helpers;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;


namespace ParkingSystem.Customers;

[AbpAuthorize(PermissionNames.Pages_Customers)]
public class CustomerAppService : AsyncCrudAppService<Customer, CustomerDto, long, PagedCustomerResultRequestDto, CreateCustomerDto, UpdateCustomerDto>, ICustomerAppService
{
  

    public CustomerAppService(IRepository<Customer, long> repository) : base(repository)
    {
     
    }

    public async Task<CustomerDto> GetMyProfileAsync()
    {
        var userId = AbpSession.UserId
            ?? throw new AbpAuthorizationException("User is not logged in.");

        var customer = await Repository.FirstOrDefaultAsync(x => x.UserId == userId);
        if (customer == null)
        {
            throw new ResourceNotFoundException("Customer profile not found for current user.");
        }

        return ObjectMapper.Map<CustomerDto>(customer);
    }

    private async Task CheckCustomerModifyAccessAsync(Customer customer)
    {
        if (await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Customers_ModifyAll))
        {
            return;
        }

        var canModifyOwn = await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Customers_ModifyOwn);
        if (!canModifyOwn)
        {
            throw new AbpAuthorizationException("You do not have permission to modify customer profile.");
        }

        var userId = AbpSession.UserId
            ?? throw new AbpAuthorizationException("User is not logged in.");

        if (customer.UserId != userId)
        {
            throw new AbpAuthorizationException(
                "You can only modify your own customer profile."
            );
        }
    }

    private async Task CheckCustomerViewAccessAsync(Customer customer)
    {
        var canViewAll = await PermissionChecker.IsGrantedAsync(
            PermissionNames.Pages_Customers_ViewAll
        );

        if (canViewAll)
            return;

        var userId = AbpSession.UserId
            ?? throw new AbpAuthorizationException("User is not logged in.");

        if (customer.UserId != userId)
        {
            throw new AbpAuthorizationException(
                "You do not have permission to view this customer."
            );
        }
    }

    protected override IQueryable<Customer> CreateFilteredQuery(
    PagedCustomerResultRequestDto input)
    {
        var query = Repository.GetAll().AsNoTracking();

        var canViewAll = PermissionChecker.IsGranted(
            PermissionNames.Pages_Customers_ViewAll
        );

        if (!canViewAll)
        {
            var userId = AbpSession.UserId
                ?? throw new AbpAuthorizationException(
                    "User is not logged in."
                );

            query = query.Where(x => x.UserId == userId);
        }

        return query.WhereIf(
            !input.Keyword.IsNullOrWhiteSpace(),
            x =>
                x.Name.Contains(input.Keyword) ||
                x.PhoneNumber.Contains(input.Keyword) ||
                (x.Email != null && x.Email.Contains(input.Keyword))
        );
    }
    public override async Task<CustomerDto> GetAsync(EntityDto<long> input)
    {
        var customer = await Repository.FirstOrDefaultAsync(x => x.Id == input.Id);

        if (customer == null)
        {
            throw new ResourceNotFoundException("Customer not found with id: " + input.Id);
        }

        await CheckCustomerViewAccessAsync(customer);

        return ObjectMapper.Map<CustomerDto>(customer);
    }
    protected override IQueryable<Customer> ApplySorting(IQueryable<Customer> query, PagedCustomerResultRequestDto input)
    {
        if (!string.IsNullOrEmpty(input.Sorting))
        {
            var sorting = SortingHelper.ValidateSorting(
           input.Sorting,
           nameof(Customer.Id),  
           nameof(Customer.Email),
           nameof(Customer.CreationTime),
           nameof(Customer.PhoneNumber),
           nameof(Customer.Name)
            );

            return query.OrderBy(sorting);
        }
        return query.OrderByDescending(x => x.Id);
    }

    public override async Task<CustomerDto> CreateAsync(CreateCustomerDto input)
    {
        var userId = AbpSession.UserId
            ?? throw new AbpAuthorizationException("User is not logged in.");

        var existingCustomer = await Repository
     .GetAll()
     .IgnoreQueryFilters()
     .FirstOrDefaultAsync(x => x.UserId == userId);

        if (existingCustomer != null)
        {
            throw new DuplicateResourceException(
                "Current user already has a customer profile."
            );
        }

        var existCustomer = await Repository
            .GetAll()
            .AnyAsync(x =>
                x.PhoneNumber == input.PhoneNumber ||
                (input.Email != null && x.Email == input.Email));

        if (existCustomer)
        {
            throw new DuplicateResourceException(
                "A customer with the same phone number or email already exists."
            );
        }

        var entity = ObjectMapper.Map<Customer>(input);
        entity.UserId = userId;

        var created = await Repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        return MapToEntityDto(created);
    }
    public override async Task<CustomerDto> UpdateAsync(UpdateCustomerDto input)
    {
        // Load entity including soft-deleted ones to detect deleted state
        var entity = await Repository.FirstOrDefaultAsync(input.Id);
        if (entity == null)
        {
            throw new ResourceNotFoundException("Customer not found with id: "+input.Id);
        }
 
        var existCustomer = await Repository
           .GetAll()
           .AnyAsync(x => x.Id != input.Id &&
             (x.PhoneNumber == input.PhoneNumber ||
               (input.Email != null && x.Email == input.Email)));

        if (existCustomer)
        {
            throw new DuplicateResourceException(
                "A customer with the same phone number or email already exists."
            );
        }
        await CheckCustomerModifyAccessAsync(entity);
       

        // Ensure we preserve UserId and only update allowed fields
        var originalUserId = entity.UserId;

        // Map incoming fields onto existing entity
        ObjectMapper.Map(input, entity);

        // Preserve UserId
        entity.UserId = originalUserId;

        await Repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        return MapToEntityDto(entity);
    }

    [AbpAuthorize(PermissionNames.Pages_Customers_ModifyAll)]
    public override async Task DeleteAsync(EntityDto<long> input)
    {
        // Load entity including soft-deleted ones so we can return proper errors
        var entity = await Repository.FirstOrDefaultAsync(input.Id);
        if (entity == null)
        {
            throw new ResourceNotFoundException("Customer not found with id: " + input.Id);
        }
        await Repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();
    }
}