
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
    [Required]
    public string TicketCode { get; set; }
    public decimal Fee { get; set; }
    public long? ParkingSpotId { get; set; }
    public ParkingSpot ParkingSpot { get; set; }
    public long ParkingAreaId { get; set; }
    public ParkingArea ParkingArea { get; set; }
    public Subscription Subscription { get; set; }
    public long? SubscriptionId { get; set; }
    public string? PlateNumber { get; set; }
    public DateTime EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? DailyRate { get; set; }
    public ParkingSessionStatus Status { get; set; }
    public ParkingSession()
    {
        EntryTime = Clock.Now;
        Status = ParkingSessionStatus.Active;
    }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public long? CheckInStaffId { get; set; }
    public Staff CheckInStaff { get; set; }
    public long? CheckOutStaffId { get; set; }
    public Staff CheckOutStaff { get; set; }
    public Customer Customer { get; set; }
    public long? CustomerId { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<ParkingSessionImage> ParkingSessionImages { get; set; } = new List<ParkingSessionImage>();

}
