using ParkingSystem.Subscriptions.Dto;
using System.Collections.Generic;

namespace ParkingSystem.Web.Models.Subscription;

public class SubscriptionListViewModel
{
    public IReadOnlyList<SubscriptionCustomerLookupDto> Customers { get; set; } = new List<SubscriptionCustomerLookupDto>();
    public IReadOnlyList<SubscriptionQuotationLookupDto> Quotations { get; set; } = new List<SubscriptionQuotationLookupDto>();
}
