
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Authorization.Users;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using ParkingSystem.Helpers;
using ParkingSystem.Staffs.Dto;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace ParkingSystem.Staffs;

[AbpAuthorize(PermissionNames.Pages_Staffs)]
public class StaffAppService: AsyncCrudAppService<Staff, StaffDto, long, PagedStaffResultRequestDto, CreateStaffDto, UpdateStaffDto>, IStaffAppService
{
    private readonly UserManager _userManager;
    private readonly IRepository<ParkingSession, long> _parkingSessionRepository;
    public StaffAppService(
        IRepository<Staff, long> repository,
        UserManager userManager,
        IRepository<ParkingSession, long> parkingSessionRepository)
        : base(repository)
    {
        _userManager = userManager;
        _parkingSessionRepository = parkingSessionRepository;
    }

    protected override IQueryable<Staff> CreateFilteredQuery(PagedStaffResultRequestDto input)


    {
        var query = Repository.GetAll().AsNoTracking();
        var canViewAll = PermissionChecker.IsGranted(
           PermissionNames.Pages_Staffs_Manager
       );
        if(!canViewAll)
        {
            var userId = AbpSession.UserId
                ?? throw new AbpAuthorizationException(
                    "User is not logged in."
                );

            query = query.Where(x => x.UserId == userId);
        }
        return query
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                x.Name.Contains(input.Keyword) ||
                x.PhoneNumber.Contains(input.Keyword) ||
                x.Email.Contains(input.Keyword) ||
                x.Address.Contains(input.Keyword))
        
         // Ngày vào làm >= From
        .WhereIf(input.HiredDateFrom.HasValue,
            x => x.HiredDate >= input.HiredDateFrom.Value)

        // Ngày vào làm <= To
        .WhereIf(input.HiredDateTo.HasValue,
            x => x.HiredDate <= input.HiredDateTo.Value)

        // Ngày sinh >= From
        .WhereIf(input.DateOfBirthFrom.HasValue,
            x => x.DateOfBirth >= input.DateOfBirthFrom.Value)

        // Ngày sinh <= To
        .WhereIf(input.DateOfBirthTo.HasValue,
            x => x.DateOfBirth <= input.DateOfBirthTo.Value)

        // Status =
        .WhereIf(input.Status.HasValue,
            x => x.Status == input.Status.Value);
        ;
    }
    protected override IQueryable<Staff> ApplySorting(IQueryable<Staff> query, PagedStaffResultRequestDto input)
    {
        if (!string.IsNullOrEmpty(input.Sorting))
        {
            var sorting = SortingHelper.ValidateSorting(
             input.Sorting,
             nameof(Staff.Id),
             nameof(Staff.Address),
             nameof(Staff.Email),
             nameof(Staff.CreationTime),
             nameof(Staff.HiredDate),
             nameof(Staff.Status),
             nameof(Staff.DateOfBirth),
             nameof(Staff.Name),
             nameof(Staff.PhoneNumber),
             nameof(Staff.Gender)
              );
            if (sorting == "Gender asc")
                return query.OrderBy(x => x.Gender);

            if (sorting == "Gender desc")
                return query.OrderByDescending(x => x.Gender);

            return query.OrderBy(sorting);
        }
        return query.OrderByDescending(x => x.Id);
    }
    private async Task EnsureStaffCanBeModifiedAsync(Staff staff)
    {
        var hasActiveSession = await _parkingSessionRepository
            .GetAll()
            .AnyAsync(x =>
                x.Status == ParkingSessionStatus.Active &&
                (x.CheckInStaffId == staff.Id ||
                 x.CheckOutStaffId == staff.Id));

        if (hasActiveSession)
        {
            throw new BusinessRuleException(
                "Cannot modify or delete this staff because they are currently handling an ongoing parking session."
            );
        }
    }

    private async Task CheckStaffModifyAccessAsync(Staff staff)
    {
        if (await PermissionChecker.IsGrantedAsync(
            PermissionNames.Pages_Staffs_Manager))
        {
            return;
        }

        var userId = AbpSession.UserId
            ?? throw new AbpAuthorizationException("User is not logged in.");

        if (staff.UserId != userId)
        {
            throw new AbpAuthorizationException(
                "You can only modify your own staff profile."
            );
        }
    }
    private async Task CheckStaffViewAccessAsync(Staff staff)
    {
        var canViewAll = await PermissionChecker.IsGrantedAsync(
            PermissionNames.Pages_Staffs_Manager
        );

        if (canViewAll)
            return;

        var userId = AbpSession.UserId
            ?? throw new AbpAuthorizationException("User is not logged in.");

        if (staff.UserId != userId)
        {
            throw new AbpAuthorizationException(
                "You do not have permission to view this staff."
            );
        }
    }
    public override async Task<StaffDto> GetAsync(EntityDto<long> input)
    {
        var staff = await Repository.FirstOrDefaultAsync(x => x.Id == input.Id);

        if (staff == null)
        {
            throw new ResourceNotFoundException("Staff not found with id: " + input.Id);
        }

        await CheckStaffViewAccessAsync(staff);

        return ObjectMapper.Map<StaffDto>(staff);
    }


    [AbpAuthorize(PermissionNames.Pages_Staffs_Manager)]
    public override async Task<StaffDto> CreateAsync(CreateStaffDto input)
    {
        // 1. Kiểm tra user tồn tại
        var user = await _userManager.GetUserByIdAsync(input.UserId);
        
        if(user == null)
        {
            throw new ResourceNotFoundException("User not found with id "+ input.UserId);
        }

        // 2. User phải có role Staff
        var roles = await _userManager.GetRolesAsync(user);

        if (!roles.Contains("Staff"))
        {
            throw new UserNotInRoleException("User does not have Staff role.");
        }

        // 3. Một user chỉ được có một Staff profile
        var existingStaffByUser = await Repository
            .GetAll()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.UserId == input.UserId);

        if (existingStaffByUser)
        {
            throw new DuplicateResourceException(
                "This user already has a Staff profile.");
        }

        // 4. Không trùng phone/email
        var existingStaff = await Repository
            .GetAll()
            .AnyAsync(x =>
                x.PhoneNumber == input.PhoneNumber ||
                (input.Email != null && x.Email == input.Email));

        if (existingStaff)
        {
            throw new DuplicateResourceException(
                "A Staff with the same phone number or email already exists.");
        }

        // 5. Tạo Staff
        var entity = ObjectMapper.Map<Staff>(input);

        entity.UserId = user.Id;

        var created = await Repository.InsertAsync(entity);

        await CurrentUnitOfWork.SaveChangesAsync();

        return MapToEntityDto(created);
    }

    [AbpAuthorize(PermissionNames.Pages_Staffs_Manager)]
    public override async Task<StaffDto> UpdateAsync(UpdateStaffDto input)
    {
        var entity = await Repository.FirstOrDefaultAsync(input.Id);
        if (entity == null)
        {
            throw new ResourceNotFoundException("Staff not found with id: " + input.Id);
        }
        var existStaff = await Repository
           .GetAll()
           .AnyAsync(x => x.Id != input.Id &&
             (x.PhoneNumber == input.PhoneNumber ||
               (input.Email != null && x.Email == input.Email)));

        if (existStaff)
        {
            throw new DuplicateResourceException(
                "A Staff with the same phone number or email already exists."
            );
        }
        ObjectMapper.Map(input,entity);

        await Repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();
        return MapToEntityDto(entity);
    }

    [AbpAuthorize(PermissionNames.Pages_Staffs_Manager)]
    public override async Task DeleteAsync(EntityDto<long> input)
    {
        var entity = await Repository.FirstOrDefaultAsync(input.Id);
        if (entity == null)
        {
            throw new ResourceNotFoundException("Staff not found with id: " + input.Id);
        }

        await EnsureStaffCanBeModifiedAsync(entity);
        await CheckStaffModifyAccessAsync(entity);
        await Repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

    }

    [AbpAuthorize(PermissionNames.Pages_Staffs_Manager)]
    public async Task<StaffDto> ChangeStatusAsync(ChangeStatusDto input)
    {
        var entity = await Repository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == input.Id);

        if (entity == null)
        {
            throw new ResourceNotFoundException(
                "Staff not found with id: " + input.Id);
        }
         await EnsureStaffCanBeModifiedAsync(entity);

        entity.Status = input.Status;

        await CurrentUnitOfWork.SaveChangesAsync();

        return MapToEntityDto(entity);
    }

    public async Task<StaffDto> ChangeProfileAsync(ChangeProfileDto input)
    {
        var userId = AbpSession.UserId
            ?? throw new AbpAuthorizationException("User is not logged in.");
        var entity = await Repository.GetAll()
            .FirstOrDefaultAsync(x => x.UserId == userId);
        if (entity == null)
        {
            throw new ResourceNotFoundException(
                "Staff profile not found for the current user."
            );
        }
        // Check for duplicate phone/email
        var existStaff = await Repository
            .GetAll()
            .AnyAsync(x => x.Id != entity.Id &&
                ( input.PhoneNumber != null && x.PhoneNumber == input.PhoneNumber ||
                 (input.Email != null && x.Email == input.Email)));
        if (existStaff)
        {
            throw new DuplicateResourceException(
                "A Staff with the same phone number or email already exists."
            );
        }
        ObjectMapper.Map(input, entity);
        await CurrentUnitOfWork.SaveChangesAsync();
        return MapToEntityDto(entity);
    }

    public async Task<StaffDto> GetMyProfileAsync()
    {
        var userId = AbpSession.UserId
            ?? throw new AbpAuthorizationException("User is not logged in.");
        var entity = await Repository.GetAll()
            .FirstOrDefaultAsync(x => x.UserId == userId);
        if (entity == null)
        {
            throw new ResourceNotFoundException(
                "Staff profile not found for the current user."
            );
        }
        return MapToEntityDto(entity);
    }


}
