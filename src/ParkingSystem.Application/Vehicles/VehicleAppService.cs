

using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;

using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using ParkingSystem.Helpers;
using ParkingSystem.Vehicles.Dto;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace ParkingSystem.Vehicles;

[AbpAuthorize(PermissionNames.Pages_Vehicles)]
public class VehicleAppService : AsyncCrudAppService<Vehicle, VehicleDto, long, PagedVehicleResultRequestDto, CreateVehicleDto, UpdateVehicleDto>, IVehicleAppService
{
    private readonly IRepository<Customer,long> _customerRepository;
    private readonly IRepository<Reservation, long> _reservationRepository;

    private readonly IRepository<ParkingSession, long> _parkingSessionRepository;
    public VehicleAppService(IRepository<Vehicle, long> repository,IRepository<Customer,long> customerRepository, IRepository<Reservation, long> reservationRepository, IRepository<ParkingSession, long> parkingSessionRepository) : base(repository)
    {
        _customerRepository = customerRepository;
        _reservationRepository = reservationRepository;
        _parkingSessionRepository = parkingSessionRepository;

    }
    private string GetVehiclePrefix(VehicleType type)
    {
        return type switch
        {
            VehicleType.Bicycle => "BC",
            VehicleType.ElectricBicycle => "EB",
            VehicleType.Motorcycle => "MC",
            VehicleType.ElectricMotorcycle => "EM",
            VehicleType.Car => "C",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
    private async Task CheckVehicleAccessAsync(Vehicle vehicle)
    {
        if (await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Vehicles_ModifyAll))
        {
            return;
        }

        var canModifyOwn = await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Vehicles_ModifyOwn);
        if (!canModifyOwn)
        {
            throw new AbpAuthorizationException("You do not have permission to modify this vehicle.");
        }

        var userId = AbpSession.UserId
            ?? throw new AbpAuthorizationException("User is not logged in.");

        var customer = await _customerRepository
            .FirstOrDefaultAsync(x => x.Id == vehicle.CustomerId);

        if (customer == null || customer.UserId != userId)
        {
            throw new AbpAuthorizationException(
                "You do not have permission to access this vehicle."
            );
        }
    }
    private async Task CheckVehicleViewAccessAsync(Vehicle vehicle)
    {
        var canViewAll = await PermissionChecker.IsGrantedAsync(
            PermissionNames.Pages_Vehicles_ViewAll
        );

        if (canViewAll)
            return;

        var userId = AbpSession.UserId
            ?? throw new AbpAuthorizationException("User is not logged in.");

        var customer = await _customerRepository.FirstOrDefaultAsync(x => x.Id == vehicle.CustomerId);
        if (customer == null || customer.UserId != userId)
        {
            throw new AbpAuthorizationException("You do not have permission to view this vehicle.");
        }
    }
    private string GenerateVehicleCode(VehicleType type)
    {
        var prefix = GetVehiclePrefix(type);
        var id = Guid.NewGuid().ToString("N")[..16].ToUpper();
        return $"{prefix}{id}";
    }

    protected override IQueryable<Vehicle> CreateFilteredQuery(PagedVehicleResultRequestDto input)
    {
        IQueryable<Vehicle> query = Repository.GetAll().AsNoTracking()
            .Include(x => x.Customer);

        var canViewAll = PermissionChecker.IsGranted(PermissionNames.Pages_Vehicles_ViewAll);
        if (!canViewAll)
        {
            var userId = AbpSession.UserId ?? throw new AbpAuthorizationException("User is not logged in.");
            query = query.Where(x => x.Customer.UserId == userId);
        }
        else
        {
            query = query.WhereIf(
                input.CustomerId.HasValue,
                x => x.CustomerId == input.CustomerId);
        }

        return query
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                x.Brand.Contains(input.Keyword) ||
                x.Color.Contains(input.Keyword) ||
                x.LicensePlate.Contains(input.Keyword) ||
                x.VehicleCode.Contains(input.Keyword))
            .WhereIf(input.VehicleType.HasValue,
                x => x.VehicleType == input.VehicleType.Value);
    }
    


    protected override IQueryable<Vehicle> ApplySorting(IQueryable<Vehicle> query, PagedVehicleResultRequestDto input)
    {
        if (!string.IsNullOrEmpty(input.Sorting))
        {
            var sorting = SortingHelper.ValidateSorting(
             input.Sorting,
             nameof(Vehicle.Id),
             nameof(Vehicle.VehicleType),
             nameof(Vehicle.Brand),
             nameof(Vehicle.Color),
             nameof(Vehicle.CustomerId),
             nameof(Vehicle.CreationTime),
             nameof(Vehicle.VehicleCode),
             nameof(Vehicle.LicensePlate),
              "customerName"
              );
            if (sorting == "customerName desc")
                return query.OrderByDescending(x => x.Customer.Name);

            if (sorting == "customerName asc")
                return query.OrderBy(x => x.Customer.Name);

            return query.OrderBy(sorting);
        }
        return query.OrderByDescending(x => x.Id);
    }

    public override async Task<VehicleDto> CreateAsync(CreateVehicleDto input)
    {
        var userId = AbpSession.UserId
            ?? throw new AbpAuthorizationException("User is not logged in.");

        long customerId;
        var canModifyAll = await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Vehicles_ModifyAll);
        if (canModifyAll && input.CustomerId.HasValue)
        {
            var targetCustomer = await _customerRepository.FirstOrDefaultAsync(input.CustomerId.Value);
            if (targetCustomer == null)
            {
                throw new ResourceNotFoundException("Customer not found with id: " + input.CustomerId.Value);
            }
            customerId = targetCustomer.Id;
        }
        else
        {
            var customer = await _customerRepository
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (customer == null)
            {
                throw new ResourceNotFoundException(
                    "Customer profile not found for current user."
                );
            }
            customerId = customer.Id;
        }

        if (!string.IsNullOrWhiteSpace(input.LicensePlate))
        {
            var duplicated = await Repository.GetAll()      
                .AnyAsync(v => v.LicensePlate == input.LicensePlate);
            if (duplicated)
            {
                throw new DuplicateResourceException("License plate already exists.");
            }
        }

        var entity = ObjectMapper.Map<Vehicle>(input);
        entity.CustomerId = customerId;
        entity.VehicleCode = GenerateVehicleCode(input.VehicleType);

        var created = await Repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        return MapToEntityDto(created);
    }
    public override async Task<VehicleDto> GetAsync(EntityDto<long> input)
    {
        var vehicle = await Repository.FirstOrDefaultAsync(input.Id);

        if (vehicle == null)
        {
            throw new ResourceNotFoundException("Vehicle not found with id: "+ input.Id);
        }

        await CheckVehicleViewAccessAsync(vehicle);

        return ObjectMapper.Map<VehicleDto>(vehicle);
    }
    public override async Task<VehicleDto> UpdateAsync(UpdateVehicleDto input)
    {
        var entity = await Repository.FirstOrDefaultAsync(input.Id);


        if (entity == null)
        {
            throw new ResourceNotFoundException("Vehicle not found with id: " + input.Id);
        }
        
        if (!string.IsNullOrWhiteSpace(input.LicensePlate))
        {
            var duplicated = await Repository.GetAll()
                       .AnyAsync(v =>v.Id != input.Id && v.LicensePlate == input.LicensePlate);
            if (duplicated)
            {
                throw new DuplicateResourceException("License plate already exists.");
            }
        }

        await CheckVehicleAccessAsync(entity);
        ObjectMapper.Map(input, entity);

        await Repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();
        return MapToEntityDto(entity);
    }
    public override async Task DeleteAsync(EntityDto<long> input)
    {
        var entity = await Repository.FirstOrDefaultAsync(input.Id);
        if (entity == null)
        {
            throw new ResourceNotFoundException("Vehicle not found with id: " + input.Id);
        }

        var isVehicleInParkingSession = await _parkingSessionRepository.GetAll()
            .AnyAsync(ps => ps.VehicleId == entity.Id && ps.Status == ParkingSessionStatus.Active);

        if (isVehicleInParkingSession)
        {
            throw new BusinessRuleException("Cannot delete vehicle that is currently in a parking session.");
        }

        await CheckVehicleAccessAsync(entity);
        await Repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

    }
    

}
