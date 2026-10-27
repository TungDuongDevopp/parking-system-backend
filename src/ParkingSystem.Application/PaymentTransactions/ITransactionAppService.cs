
using Abp.Application.Services.Dto;
using ParkingSystem.PaymentTransactions.Dto;
using System.Threading.Tasks;

namespace ParkingSystem.PaymentTransactions;

public interface ITransactionAppService
{
    Task<TransactionDto> GetAsync(EntityDto<long> input);
    Task<PagedResultDto<TransactionDto>> GetAllAsync(PagedTransactionResultRequest input);
    Task<TransactionDto> ProcessPaymentResultAsync(ProcessPaymentResultInput input);
    Task<TransactionDto> RetryPaymentAsync(RetryPaymentInput input);
}
