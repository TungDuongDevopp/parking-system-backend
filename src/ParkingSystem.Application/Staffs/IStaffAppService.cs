

using Abp.Application.Services;
using ParkingSystem.Staffs.Dto;
using System.Threading.Tasks;

namespace ParkingSystem.Staffs;

public interface IStaffAppService : IAsyncCrudAppService<StaffDto,long,PagedStaffResultRequestDto,CreateStaffDto,UpdateStaffDto>
{
    Task<StaffDto> ChangeStatusAsync(ChangeStatusDto input);
    Task<StaffDto> ChangeProfileAsync(ChangeProfileDto input);
    Task<StaffDto> GetMyProfileAsync();
}
