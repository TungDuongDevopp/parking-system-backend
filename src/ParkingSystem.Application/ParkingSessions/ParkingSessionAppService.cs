

using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using ParkingSystem.Authorization;
using ParkingSystem.Entities;
using ParkingSystem.ParkingSessions.Dto;
using System.Threading.Tasks;

namespace ParkingSystem.ParkingSessions;

[AbpAuthorize(PermissionNames.Pages_ParkingSessions)]
public class ParkingSessionAppService : ParkingSystemAppServiceBase, IParkingSessionAppService
{
    private readonly IRepository<ParkingSession, long> _repository;
    private readonly IRepository<Reservation, long> _reservationRepository;
    private readonly IRepository<ParkingArea, long> _parkingAreaRepository;
    private readonly IRepository<ParkingSpot, long> _parkingSpotRepository;
    private readonly IRepository<Customer, long> _customerRepository;
    public ParkingSessionAppService(IRepository<ParkingSession, long> repository)
    {
        _repository = repository;
    }
    public Task<ParkingSessionDto> CheckInAsync(ParkingSessionCheckInDto input)
    {
        throw new System.NotImplementedException();
    }

    public Task<ParkingSessionDto> CheckOutAsync(ParkingSessionCheckOutDto input)
    {
        throw new System.NotImplementedException();
    }

    public Task<PagedResultDto<ParkingSessionDto>> GetAllAsync(PagedParkingSessionResultRequestDto input)
    {
        throw new System.NotImplementedException();
    }

    public Task<ParkingSessionDto> GetAsync(Entity<long> input)
    {
        throw new System.NotImplementedException();
    }
}
