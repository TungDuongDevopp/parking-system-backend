

using Abp.Application.Services;
using ParkingSystem.Staffs.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ParkingSystem.Staffs;

public interface IStaffAppService : IAsyncCrudAppService<StaffDto,long,PagedStaffResultRequestDto,CreateStaffDto,UpdateStaffDto>
{
    Task<StaffDto> ChangeStatusAsync(ChangeStatusDto input);
    Task<StaffDto> ChangeProfileAsync(ChangeProfileDto input);
    Task<StaffDto> GetMyProfileAsync();

    /// <summary>
    /// Returns users that have the Staff role but do NOT yet have a Staff profile.
    /// Used by the React frontend when creating a new Staff record.
    /// Requires Pages_Staffs_Manager permission.
    /// </summary>
    Task<List<StaffUserLookupDto>> GetAvailableUsersAsync();
}
