

using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
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

    public VehicleType VehicleType { get; set; }
    public int Capacity { get; set; }
    public ParkingMode ParkingMode { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Location { get; set; }

    [StringLength(255)]
    public string? Description{ get; set; }
    public ParkingAreaStatus Status { get; set; }
    public int CurrentOccupancy { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }

    public ICollection<ParkingSpot> ParkingSpots { get; set; } = new List<ParkingSpot>();
    public bool IsDeleted { get; set; }

    public ParkingArea() => Status = ParkingAreaStatus.Active;
   
}
