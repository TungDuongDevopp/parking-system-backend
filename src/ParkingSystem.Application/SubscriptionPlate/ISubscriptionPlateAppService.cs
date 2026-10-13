
using Abp.Application.Services.Dto;
using ParkingSystem.SubscriptionPlate.Dto;
using System.Threading.Tasks;

namespace ParkingSystem.SubscriptionPlate;
public interface ISubscriptionPlateAppService
{
    Task<SubscriptionPlateDto> GetAsync(EntityDto<long> input);
    Task<PagedResultDto<SubscriptionPlateDto>> GetAllAysnc(PagedSubscriptionPlateResultRequestDto input);
    Task<SubscriptionPlateDto> ChangePlateAsync(ChangePlateDto input);
    Task<SubscriptionPlateDto> CreateAsync(CreatePlateDto input);
    Task<SubscriptionPlateDto> AddPlateAsync(CreatePlateDto input);
}
