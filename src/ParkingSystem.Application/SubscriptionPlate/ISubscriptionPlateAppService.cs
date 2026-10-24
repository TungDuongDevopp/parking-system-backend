
using Abp.Application.Services.Dto;
using ParkingSystem.SubscriptionPlate.Dto;
using System.Threading.Tasks;

namespace ParkingSystem.SubscriptionPlate;
public interface ISubscriptionPlateAppService
{
    Task<SubscriptionPlateDto> GetAsync(EntityDto<long> input);
    Task<PagedResultDto<SubscriptionPlateDto>> GetAllAsync(PagedSubscriptionPlateResultRequestDto input);
    Task<SubscriptionPlateDto> ChangePlateAsync(ChangePlateDto input);
    Task<SubscriptionPlateDto> AddPlateAsync(AddPlateDto input);
}
