
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
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; }
       
        [StringLength(255)] 
       
        public string? Email { get; set; }
       
        public DateTime CreationTime { get; set; }
       
        public Customer()
        {
            CreationTime = Clock.Now;
            IsDeleted = false;
        }

        public long UserId { get; set; }

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public DateTime? LastModificationTime { get; set; }
        public bool IsDeleted { get; set; }
    }
}
