using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Timing;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using System;
using System.ComponentModel.DataAnnotations;

public class Reservation :
    Entity<long>,
    IHasCreationTime,
    IHasModificationTime,
    ISoftDelete
{
    [Required]
    public long CustomerId { get; set; }
    public Customer Customer { get; set; }

    [Required]
    public long ParkingAreaId { get; set; }
    public ParkingArea ParkingArea { get; set; }

    public long? ParkingSpotId { get; set; }
    public ParkingSpot? ParkingSpot { get; set; }


    [Required]
    public DateTime ReservedAt { get; set; }

    [Required]
    public DateTime ExpireAt { get; set; }

    [Required]
    public ReservationStatus Status { get; set; }

    public DateTime CreationTime { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public bool IsDeleted { get; set; }

    public Reservation()
    {
        CreationTime = Clock.Now;
        IsDeleted = false;
        Status = ReservationStatus.Reserved;
    }
}