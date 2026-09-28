
using Abp.Application.Services.Dto;
using ParkingSystem.Quotations.Dto;
using ParkingSystem.Subscriptions.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;


public interface ISubscriptionAppService 
{
    Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto input);

    Task<SubscriptionDto> GetAsync(EntityDto<long> input);

    Task<PagedResultDto<SubscriptionDto>> GetAllAsync(
        PagedSubscriptionResultRequestDto input);

    /// <summary>
    /// Returns the calling customer's current subscription (pending or inUse), or null.
    /// </summary>
    Task<SubscriptionDto> GetMyCurrentSubscriptionAsync();

    /// <summary>
    /// Returns quotations that customers are allowed to purchase (Week / Month / Year only).
    /// </summary>
    Task<List<QuotationDto>> GetAvailableQuotationsAsync();
}
