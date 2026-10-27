

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System;

namespace ParkingSystem.Payments.Dto;
public class PaymentDto: EntityDto<long>
{
    public decimal Amount { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Description { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodName => PaymentMethod.ToString();
    public PaymentStatus PaymentStatus { get; set; }
    public string PaymentStatusName => PaymentStatus.ToString();
    public PaymentType PaymentType { get; set; }
    public string PaymentTypeName => PaymentType.ToString();
    public long? SubscriptionId { get; set; }
    public long? ParkingSessionId { get; set; }

}
