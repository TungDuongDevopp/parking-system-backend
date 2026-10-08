

using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Timing;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Entities;

public class Staff : Entity<long>, IHasCreationTime, IHasModificationTime, ISoftDelete
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
 
    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; }
   
    [StringLength(255)]
    public string? Email { get; set; }

    public bool? Gender { get; set; } // true for male, false for female

    public DateTime HiredDate { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public DateTime CreationTime { get; set; }
    
    public StaffStatus Status { get; set; }

    [StringLength(1023)]
    public string? Address { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public long UserId { get; set; }
    public bool IsDeleted { get; set; }

    public Staff()
    {
        CreationTime = Clock.Now;
        Status = StaffStatus.Active; // Default status
        IsDeleted = false;
    }

    public ICollection<ParkingSession> CheckInParkingSessions { get; set; } = new List<ParkingSession>();
    public ICollection<ParkingSession> CheckOutParkingSessions { get; set; } = new List<ParkingSession>();
}
