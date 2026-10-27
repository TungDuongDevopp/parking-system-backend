
using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.PaymentTransactions.Dto;
public class PagedTransactionResultRequest : PagedResultRequestDto, ISortedResultRequest
{
    public string Keyword { get; set; }
    public string Sorting { get; set; }
    public long? PaymentId { get; set; }
    public decimal ? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }

    [EnumDataType(typeof (TransactionStatus))]
    public TransactionStatus? Status { get; set; }

    [EnumDataType(typeof(TransactionType))]
    public TransactionType? Type { get; set; }
    public DateTime? TransactionTimeFrom { get; set; }
    public DateTime? TransactionTimeTo { get; set; }
}
