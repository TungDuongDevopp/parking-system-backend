
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Timing;
using ParkingSystem.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Entities
{
    public class Payment: Entity<long>, IHasCreationTime, IHasModificationTime
    {
        [Required]
        public PaymentMethod PaymentMethod { get; set; }
       
        [Required]       
        public PaymentStatus PaymentStatus { get; set; }

        [Required]
        public decimal ExpectedAmount { get; set; }

        [Required]
        public decimal ReceivedAmount { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime? LastModificationTime { get; set; }

        public DateTime? PaidAt { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

      
        public long? SubscriptionId { get; set; }

        public long? ParkingSessionId { get; set; }

        public PaymentType PaymentType { get; set; }
        public ParkingSession ParkingSession { get; set; }

        public Subscription Subscription { get; set; }
        public DateTime ExpiresAt { get; set; }

        public Payment()
        {
            CreationTime = Clock.Now;
            PaymentStatus = PaymentStatus.Pending;
            ExpiresAt = Clock.Now.AddHours(24);
        }

        public ICollection<PaymentTransaction> PaymentTransactions { get; set; }= new List<PaymentTransaction>();
    }
}
