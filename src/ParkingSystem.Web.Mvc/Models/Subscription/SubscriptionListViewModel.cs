using System.Collections.Generic;
using System.Globalization;
using ParkingSystem.Entities.Enums;

namespace ParkingSystem.Web.Models.Subscription;

public class SubscriptionCustomerLookupDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string PhoneNumber { get; set; }

    public string DisplayText => string.IsNullOrWhiteSpace(PhoneNumber)
        ? Name
        : $"{Name} ({PhoneNumber})";
}

public class SubscriptionQuotationLookupDto
{
    public long Id { get; set; }
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

public class SubscriptionListViewModel
{
    public IReadOnlyList<SubscriptionCustomerLookupDto> Customers { get; set; } = new List<SubscriptionCustomerLookupDto>();
    public IReadOnlyList<SubscriptionQuotationLookupDto> Quotations { get; set; } = new List<SubscriptionQuotationLookupDto>();
}
