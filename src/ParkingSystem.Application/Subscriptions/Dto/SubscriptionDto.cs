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

    public long QuotationId { get; set; }
    public int Duration { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public string DurationUnitName => DurationUnit.ToString();

    public decimal Price { get; set; }

    public long CustomerId { get; set; }
    public string CustomerName { get; set; }

    public VehicleType VehicleType { get; set; }
    public string VehicleTypeName => VehicleType.ToString();
}
