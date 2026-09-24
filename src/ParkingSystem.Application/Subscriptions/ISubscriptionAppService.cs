
using Abp.Application.Services.Dto;
using ParkingSystem.Subscriptions.Dto;
using System.Threading.Tasks;


public interface ISubscriptionAppService 
{
    Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto input);

    Task<SubscriptionDto> GetAsync(EntityDto<long> input);

    Task<PagedResultDto<SubscriptionDto>> GetAllAsync(
        PagedSubscriptionResultRequestDto input);
}
