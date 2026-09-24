

using ParkingSystem.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Subscriptions.Dto;

public class CreateSubscriptionDto
{
    [NotPast]
    public DateTime StartTime { get; set; }

    [Required]
    public long QuotationId { get; set; }
}
