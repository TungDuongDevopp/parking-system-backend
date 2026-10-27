using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System;


namespace ParkingSystem.Subscriptions.Dto;

public class SubscriptionDto : EntityDto<long>
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public SubscriptionStatus Status { get; set; }
    public string SubscriptionStatusName => Status.ToString();
    public int Duration { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public string DurationUnitName => DurationUnit.ToString();
    public decimal TotalAmount { get; set; }
    public decimal Price { get; set; }
    public long CustomerId { get; set; }
    public string CustomerName { get; set; }
    public VehicleType VehicleType { get; set; }
    public string VehicleTypeName => VehicleType.ToString();
    public long? PaymentId { get; set; }
    public string? TransactionCode { get; set; }
    public DateTime? PaymentExpiresAt { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
}

