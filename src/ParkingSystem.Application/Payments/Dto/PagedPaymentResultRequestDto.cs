

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Payments.Dto;

public class PagedPaymentResultRequestDto: PagedResultRequestDto, ISortedResultRequest
{
    public string Keyword { get; set; }
    public string Sorting { get; set; }

    [GreaterThanZero]
    [EnumDataType (typeof(PaymentMethod))]
    public PaymentMethod? PaymentMethod { get; set; }

    [EnumDataType(typeof(PaymentStatus))]
    public PaymentStatus? PaymentStatus { get; set; }

    [EnumDataType(typeof(PaymentType))]
    public PaymentType? PaymentType { get; set; }

    [GreaterThanZero]
    public long? SubscriptionId { get; set; }
    
    [GreaterThanZero]   
    public long? ParkingSessionId { get; set; }

    [GreaterThanZero]
    public decimal? AmountFrom { get; set; }
    [GreaterThanZero]
    public decimal? AmountTo { get; set; }

    public DateTime? ExpiresFrom { get; set; }
    public DateTime? ExpiresTo { get; set; }
    public DateTime? PaidAtFrom { get; set; }
    public DateTime? PaidAtTo { get; set; }
}
