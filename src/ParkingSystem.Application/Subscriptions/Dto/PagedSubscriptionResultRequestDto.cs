using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Subscriptions.Dto;

public class PagedSubscriptionResultRequestDto :PagedResultRequestDto, ISortedResultRequest
{
    public string Keyword { get; set; }
    public string Sorting { get; set; }

    [EnumDataType(typeof(SubscriptionStatus))]
    public SubscriptionStatus? Status { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [GreaterThanZero]
    public long? QuotationId { get; set; }

    [GreaterThanZero]
    public long? CustomerId { get; set; }


}
