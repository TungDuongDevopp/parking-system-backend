

using Abp.Application.Services.Dto;
using ParkingSystem.Payments.Dto;
using System.Threading.Tasks;

namespace ParkingSystem.Payments;

public interface IPaymentAppService
{
    Task<PaymentDto> GetAsync(EntityDto<long> input);
    Task<PagedResultDto<PaymentDto>> GetAllAsync(PagedPaymentResultRequestDto input);
    Task<PaymentLookUpDto> GetPaymentLookUpAsync();
}
