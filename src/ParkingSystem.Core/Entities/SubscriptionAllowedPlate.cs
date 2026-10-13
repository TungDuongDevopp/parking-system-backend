

using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Entities;

public class SubscriptionAllowedPlate : Entity<long>, IHasCreationTime, IHasModificationTime
{
    public long SubscriptionId { get; set; }
    public Subscription Subscription { get; set; }
   
    [Required]
    [StringLength(12)]
    public string PlateNumber { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }

}

