

using Abp.Application.Services.Dto;
using System.Collections.Generic;
namespace ParkingSystem.PaymentTransactions.Dto;


public class TransactionPaymentLookupDto: EntityDto<long>
{
    public string Method { get; set; }
    public string Type { get; set; }

    public string Display => $"{Id} ({Method}) - {Type}";
}
public class TransactionLookupDto
{
    public List<TransactionPaymentLookupDto> Payments { get; set; }
}
