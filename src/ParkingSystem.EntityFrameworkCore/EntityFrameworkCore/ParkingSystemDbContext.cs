using Abp.Zero.EntityFrameworkCore;
using ParkingSystem.Authorization.Roles;
using ParkingSystem.Authorization.Users;
using ParkingSystem.MultiTenancy;
using ParkingSystem.Entities;
using Microsoft.EntityFrameworkCore;
namespace ParkingSystem.EntityFrameworkCore;

public class ParkingSystemDbContext(DbContextOptions<ParkingSystemDbContext> options) : AbpZeroDbContext<Tenant, Role, User, ParkingSystemDbContext>(options)
{
    /* Define a DbSet for each entity of the application */
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Staff> Staffs { get; set; }

    public DbSet<ParkingArea> ParkingAreas { get; set; }

    public DbSet<ParkingSpot> ParkingSpots { get; set; }

    public DbSet<Quotation> Quotations { get; set; }

    public DbSet<Subscription> Subscriptions { get; set; }

    public DbSet<ParkingSession> ParkingSessions { get; set; }

    public DbSet<Payment> Payments { get; set; }

    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public DbSet<Reservation> Reservations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Customer entity configuration
        modelBuilder.Entity<Customer>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<Customer>(c => c.UserId);
       
        modelBuilder.Entity<Customer>()
            .HasIndex(x => x.UserId)
            .IsUnique()
             .HasFilter("[IsDeleted] = 0"); ;
            

        modelBuilder.Entity<Customer>()
            .HasIndex(x => x.PhoneNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0"); ;
        modelBuilder.Entity<Customer>()
            .HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [Email] IS NOT NULL");

        //Staff entity configuration
        modelBuilder.Entity<Staff>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<Staff>(s => s.UserId);

        modelBuilder.Entity<Staff>()
            .HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<Staff>()
            .HasIndex(x => x.PhoneNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        modelBuilder.Entity<Staff>()
            .HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [Email] IS NOT NULL");


        //ParkingArea entity configuration
        modelBuilder.Entity<ParkingArea>()
            .HasIndex(p => p.ParkingCode)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        //ParkingSpot entity configuration
        modelBuilder.Entity<ParkingSpot>()
             .HasIndex(s => new { s.ParkingAreaId, s.SpotCode })
             .IsUnique()
             .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<ParkingSpot>()
            .HasOne(p => p.ParkingArea)
            .WithMany(pa => pa.ParkingSpots)
            .HasForeignKey(p => p.ParkingAreaId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);


        //Quotation entity configuration
        modelBuilder.Entity<Quotation>()
            .HasIndex(q => new { q.VehicleType, q.Duration, q.DurationUnit })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        //Subscription entity configuration

        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.Customer)
            .WithMany(c => c.Subscriptions)
            .HasForeignKey(s => s.CustomerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.Quotation)
            .WithMany()
            .HasForeignKey(s => s.QuotationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        //ParkingSession entity configuration
        modelBuilder.Entity<ParkingSession>()
            .HasIndex(ps => ps.TicketCode)
            .IsUnique();
          
        modelBuilder.Entity<ParkingSession>()
            .HasOne(ps => ps.ParkingSpot)
            .WithMany()
            .HasForeignKey(ps => ps.ParkingSpotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ParkingSession>()
           .HasOne(pa => pa.ParkingArea)
           .WithMany()
           .HasForeignKey(ps => ps.ParkingAreaId)
           .IsRequired()
           .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<ParkingSession>()
            .HasOne(ps => ps.Quotation)
            .WithMany()
            .HasForeignKey(ps => ps.QuotationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ParkingSession>()
            .HasOne(ps => ps.Subscription)
            .WithMany(s => s.ParkingSessions)
            .HasForeignKey(ps => ps.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ParkingSession>()
            .HasOne(ps => ps.CheckInStaff)
            .WithMany(s => s.CheckInParkingSessions)
            .HasForeignKey(ps => ps.CheckInStaffId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ParkingSession>()
            .HasOne(ps => ps.CheckOutStaff)
            .WithMany(s => s.CheckOutParkingSessions)
            .HasForeignKey(ps => ps.CheckOutStaffId)
            .OnDelete(DeleteBehavior.Restrict);


        //Payment entity configuration
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Subscription)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.ParkingSession)
            .WithMany(ps => ps.Payments)
            .HasForeignKey(p => p.ParkingSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        //PaymentTransaction entity configuration
        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.Payment)
            .WithMany(p => p.PaymentTransactions)
            .HasForeignKey(pt => pt.PaymentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentTransaction>()
            .HasIndex(pt => pt.TransactionCode)
            .IsUnique();

        //ParkingSessionImage
        modelBuilder.Entity<ParkingSessionImage>()
            .HasOne(s => s.ParkingSession)
            .WithMany(i => i.ParkingSessionImages)
            .HasForeignKey(s => s.ParkingSessionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        //Money Configuration
        modelBuilder.Entity<Quotation>().Property(q => q.Price).HasPrecision(18, 2);
        modelBuilder.Entity<ParkingSession>().Property(ps => ps.Fee).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().Property(p => p.ExpectedAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().Property(p => p.ReceivedAmount).HasPrecision(18, 2);
        modelBuilder.Entity<PaymentTransaction>().Property(pt => pt.Amount).HasPrecision(18, 2);

        //Reservation entity configuration

        modelBuilder.Entity<Reservation>()
            .HasOne(c => c.Customer)
            .WithMany(r => r.Reservations)
            .HasForeignKey(c => c.CustomerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Reservation>()
            .HasOne(r=>r.ParkingArea)
            .WithMany()
            .HasForeignKey(r=>r.ParkingAreaId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.ParkingSpot)
            .WithMany()
            .HasForeignKey(r => r.ParkingSpotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => r.CustomerId)
            .HasDatabaseName("UX_Reservations_ActiveCustomer")
            .IsUnique()
            .HasFilter("[Status] = 1 AND [IsDeleted] = 0");

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => r.ParkingSpotId)
            .HasDatabaseName("UX_Reservations_ActiveParkingSpot")
            .IsUnique()
            .HasFilter("[ParkingSpotId] IS NOT NULL AND [Status] = 1 AND [IsDeleted] = 0");


        modelBuilder.Entity<ParkingSession>()
            .HasIndex(ps => ps.ParkingSpotId)
            .HasDatabaseName("UX_ParkingSessions_ActiveParkingSpot")
            .IsUnique()
            .HasFilter("[ParkingSpotId] IS NOT NULL AND [ExitTime] IS NULL");

        modelBuilder.Entity<ParkingSession>()
            .HasIndex(ps => ps.PlateNumber)
            .HasDatabaseName("UX_ParkingSessions_ActivePlateNumber")
            .IsUnique()
            .HasFilter("[PlateNumber] IS NOT NULL  AND [ExitTime] IS NULL");
    }
}
