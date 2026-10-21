
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Timing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ParkingSystem.Entities
{
    [Table("Customers")]
    public class Customer : Entity<long>, IHasCreationTime, IHasModificationTime, ISoftDelete

    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; }
        
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
       
        [Required]
        [StringLength(256)] 
        public string Email { get; set; }
        public DateTime CreationTime { get; set; }
        public long UserId { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<ParkingSession> ParkingSessions { get; set; } = new List<ParkingSession>();
        public DateTime? LastModificationTime { get; set; }
        public bool IsDeleted { get; set; }
    }
}
