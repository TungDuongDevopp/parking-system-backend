

using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Timing;
using ParkingSystem.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Entities;

public class ParkingArea : Entity<long>, IHasCreationTime, IHasModificationTime, ISoftDelete
{
    [Required]
    [StringLength(30)]
    public string ParkingCode { get; set; }

    [Required]
    [StringLength(30)]
    public string Name{ get; set; }

    [Required]
    public VehicleType VehicleType { get; set; }

    [Required]
    public int Capacity { get; set; }

    [Required]
    public ParkingMode ParkingMode { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Location { get; set; }

    [StringLength(255)]
    public string Description{ get; set; }

    [Required]
    public ParkingAreaStatus Status { get; set; }

    public int CurrentOccupancy { get; set; }

    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }

    public ICollection<ParkingSpot> ParkingSpots { get; set; } = new List<ParkingSpot>();
    public bool IsDeleted { get; set; }


    public ParkingArea()
    {
        CreationTime = Clock.Now;
        IsDeleted = false;
        Status = ParkingAreaStatus.Active;
    }
}
