
using Microsoft.EntityFrameworkCore;
using PG_Backend.Models;

namespace PG_Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<PGProperty> Properties { get; set; }
        public DbSet<Supervisor> Supervisors { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<GuestStay> GuestStays { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<PropertySetting> PropertySettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Seed Super Admin User
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 100,
                Username = "adityarajat",
                Mobile = "adityarajat",
                Password = "adityarajat",
                Role = UserRole.SuperAdmin,
                IsActive = true
            });

            // Seed Admin User
            modelBuilder.Entity<User>().HasData(new User 
            { 
                Id = 1, 
                Username = "admin",
                Mobile = "admin", 
                Password = "admin", 
                Role = UserRole.Admin, 
                IsActive = true 
            });

            // Seed First Property for the default Admin
            modelBuilder.Entity<PGProperty>().HasData(new PGProperty
            {
                Id = 1,
                Name = "Default PG",
                Address = "Main Road, City",
                OwnerName = "Aditya Koganti",
                AdminId = 1
            });

            // Seed Property Settings for the default Property
            modelBuilder.Entity<PropertySetting>().HasData(
                new PropertySetting { Id = 1, PropertyId = 1, Key = "UpiId", Value = "9346765275@ybl" },
                new PropertySetting { Id = 2, PropertyId = 1, Key = "UpiName", Value = "Ramesh Kumar" }
            );

            // Resolve "Multiple Cascade Paths" error for SQL Server
            modelBuilder.Entity<Guest>()
                .HasOne(g => g.Property)
                .WithMany()
                .HasForeignKey(g => g.PropertyId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Supervisor>()
                .HasOne(s => s.Property)
                .WithMany()
                .HasForeignKey(s => s.PropertyId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<GuestStay>()
                .HasOne(s => s.Room)
                .WithMany()
                .HasForeignKey(s => s.RoomId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<GuestStay>()
                .HasOne(s => s.Guest)
                .WithMany()
                .HasForeignKey(s => s.GuestId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Guest)
                .WithMany()
                .HasForeignKey(t => t.GuestId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.Guest)
                .WithMany()
                .HasForeignKey(c => c.GuestId)
                .OnDelete(DeleteBehavior.NoAction);

            // Precision for decimals
            modelBuilder.Entity<GuestStay>().Property(p => p.RentAmount).HasPrecision(18, 2);
            modelBuilder.Entity<GuestStay>().Property(p => p.AdvanceAmount).HasPrecision(18, 2);
            modelBuilder.Entity<GuestStay>().Property(p => p.PerDayRent).HasPrecision(18, 2);
            modelBuilder.Entity<Transaction>().Property(p => p.Amount).HasPrecision(18, 2);
        }
    }
}
