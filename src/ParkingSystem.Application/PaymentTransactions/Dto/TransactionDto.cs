using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System;


namespace ParkingSystem.PaymentTransactions.Dto;

public class TransactionDto: EntityDto<long>
{
    public string TransactionCode { get; set; }
    public decimal Amount { get; set; }
    public TransactionStatus Status { get; set; }
    public string TransactionStatusName => Status.ToString();
    public TransactionType Type { get; set; }
    public string TransactionTypeName => Type.ToString();
    public DateTime? TransactionTime { get; set; }
    public long? PaymentId { get; set; }
    

}
