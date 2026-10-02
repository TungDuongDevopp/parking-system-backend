
using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System.Collections.Generic;
using System.Globalization;

namespace ParkingSystem.Subscriptions.Dto;

public class SubscriptionCustomerLookupDto: EntityDto<long>
{
    public string Name { get; set; }
    public string PhoneNumber { get; set; }

    public string DisplayText => string.IsNullOrWhiteSpace(PhoneNumber)
        ? Name
        : $"{Name} ({PhoneNumber})";
}
public class SubscriptionQuotationLookupDto: EntityDto<long>
{
    public VehicleType VehicleType { get; set; }
    public int Duration { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public decimal Price { get; set; }

    public string DisplayText
    {
        get
        {
            var unitStr = Duration > 1 ? $"{DurationUnit}s" : $"{DurationUnit}";
            return $"{VehicleType} - {Duration} {unitStr} - {Price.ToString("#,##0", CultureInfo.InvariantCulture)}";
        }
    }
}
public class SubscriptionLookUpDto
{
    public List<SubscriptionCustomerLookupDto> Customers { get; set; }
    public List<SubscriptionQuotationLookupDto> Quotations { get; set; }
}
