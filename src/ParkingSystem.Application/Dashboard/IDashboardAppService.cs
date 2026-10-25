using Abp.Application.Services;
using ParkingSystem.Dashboard.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ParkingSystem.Dashboard;

public interface IDashboardAppService : IApplicationService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();

    Task<List<ParkingActivityDto>> GetParkingActivityAsync(GetParkingActivityInput input);

    Task<List<ParkingAreaOccupancyDto>> GetParkingAreaOccupancyAsync();

    Task<List<RecentActivityDto>> GetRecentActivitiesAsync(GetRecentActivitiesInput input);
}
