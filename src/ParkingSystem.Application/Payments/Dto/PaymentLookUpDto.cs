

using Abp.Application.Services.Dto;
using System.Collections.Generic;

namespace ParkingSystem.Payments.Dto;

public class PaymentSessionLookupDto: EntityDto<long>
{
    public string PlateNumber { get; set; }
    public string NameStaffIn { get; set; }
    public string NameStaffOut { get; set; }
    public string DisplayText => string.IsNullOrWhiteSpace(PlateNumber)
        ? $"In: {NameStaffIn} - Out: {NameStaffOut}"
        : $"{PlateNumber} In: {NameStaffIn} - Out: {NameStaffOut}";

}
public class PaymentSubscriptionLookUpDto: EntityDto<long>
{
    public string CustomerName { get; set; }
    public long QuotationId { get; set; }
    public string DisplayText => $"{CustomerName} - {QuotationId}";

}
public class PaymentLookUpDto
{
    public List<PaymentSessionLookupDto> Sessions { get; set; }

    public List<PaymentSubscriptionLookUpDto> Subscriptions { get; set; }
}
