

using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Timing;
using ParkingSystem.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Entities;

public class ParkingSession : Entity<long>, IHasCreationTime, IHasModificationTime
{
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }

    [Required]
    public string TicketCode { get; set; }

    public decimal Fee { get; set; }
    
    public PaymentMethod? PaymentMethod { get; set; }

    public long? ParkingSpotId { get; set; }

    public ParkingSpot ParkingSpot { get; set; }

    [Required]
    public long ParkingAreaId { get; set; }

    public ParkingArea ParkingArea { get; set; }


    public Quotation Quotation { get; set; }

    public long? QuotationId { get; set; }

    public Subscription Subscription { get; set; }

    public long? SubscriptionId { get; set; }

    public string? PlateNumber { get; set; }

    [Required]
    public string EntryImageUrl { get; set; }
    public string? ExitImageUrl { get; set; }

    [Required]
    public DateTime EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }

    [Required]
    public ParkingSessionStatus Status { get; set; }

    public ParkingSession()
    {
        CreationTime = Clock.Now;
        EntryTime = Clock.Now;
        Status = ParkingSessionStatus.Active;
    }


    public long? CheckInStaffId { get; set; }
    public Staff CheckInStaff { get; set; }

    public long? CheckOutStaffId { get; set; }
    public Staff CheckOutStaff { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

}
