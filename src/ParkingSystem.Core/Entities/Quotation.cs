

using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Timing;
using ParkingSystem.Entities.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Entities;

public class Quotation : Entity<long>, IHasCreationTime, IHasModificationTime,ISoftDelete

{
    public VehicleType VehicleType { get; set; }

    public int Duration { get; set; }

    public DurationUnit DurationUnit { get; set; }
    
    public decimal Price { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public bool IsDeleted { get; set; }

    public Quotation()
    {
        CreationTime = Clock.Now;
        IsDeleted = false;
    }
}
