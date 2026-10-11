
using Abp.Application.Services.Dto;
using Abp.Domain.Entities;
using ParkingSystem.ParkingSessions.Dto;
using System.Threading.Tasks;

namespace ParkingSystem.ParkingSessions;
public interface IParkingSessionAppService
{
    Task<ParkingSessionDto> GetAsync(EntityDto<long> input);
    Task<PagedResultDto<ParkingSessionDto>> GetAllAsync(PagedParkingSessionResultRequestDto input);
    Task<CheckInResultDto> CheckInAsync(ParkingSessionCheckInDto input);
    Task<ParkingSessionDto> CheckOutAsync(ParkingSessionCheckOutDto input);
}
