
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
        public DbSet<Supervisor> Supervisors { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Seed Admin ? 
            // Better to let user register or seed manually. 
            // For MVP:
            modelBuilder.Entity<User>().HasData(new User 
            { 
                Id = 1, 
                Mobile = "admin", 
                Password = "admin", 
                Role = UserRole.Admin, 
                IsActive = true 
            });
        }
    }
}
