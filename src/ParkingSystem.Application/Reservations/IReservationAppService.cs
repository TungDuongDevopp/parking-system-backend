

using Abp.Application.Services.Dto;
using ParkingSystem.Reservations.Dto;
using System.Threading.Tasks;

namespace ParkingSystem.Reservations;

public interface IReservationAppService
{
    Task<ReservationDto> CreateAsync(CreateReservationDto input);

    Task<ReservationDto> GetAsync(EntityDto<long> input);

    Task<PagedResultDto<ReservationDto>> GetAllAsync(
        PagedReservationResultRequestDto input);

    Task<ReservationDto> Canceled(EntityDto<long> input);

    Task<ReservationLookUpDto> GetReservationLookUpAsync();
}
