
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Timing;
using ParkingSystem.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Entities;
public class Subscription: Entity<long>, IHasCreationTime, IHasModificationTime


{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public SubscriptionStatus Status { get; set; }
    public Quotation Quotation { get; set; }
    public long QuotationId { get; set; }
    public Customer Customer { get; set; }
    public long CustomerId { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public decimal TotalAmount { get; set; }
    public Subscription() => Status = SubscriptionStatus.pending;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<ParkingSession> ParkingSessions { get; set; } = new List<ParkingSession>();
}
