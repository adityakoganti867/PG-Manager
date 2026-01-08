
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PG_Backend.Models
{
    public enum UserRole
    {
        Admin,
        Supervisor,
        Guest,
        SuperAdmin
    }

    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty; // Used as Login ID
        public string Mobile { get; set; } = string.Empty; 
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class PGProperty
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        
        public int AdminId { get; set; }
        [ForeignKey("AdminId")]
        public User? Admin { get; set; }
    }

    public class Supervisor
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; }
        
        public int PropertyId { get; set; }
        [ForeignKey("PropertyId")]
        public PGProperty? Property { get; set; }
    }

    public class Room
    {
        [Key]
        public int Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public int SharingType { get; set; } // 1, 2, 3, etc.
        public string RoomType { get; set; } = "Non-AC"; // "AC" or "Non-AC"
        public int TotalBeds { get; set; }
        public int AvailableBeds { get; set; }

        public int PropertyId { get; set; }
        [ForeignKey("PropertyId")]
        public PGProperty? Property { get; set; }
    }

    public class Guest
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Occupation { get; set; } = string.Empty;

        public int PropertyId { get; set; }
        [ForeignKey("PropertyId")]
        public PGProperty? Property { get; set; }
    }

    public class GuestStay
    {
        [Key]
        public int Id { get; set; }
        
        public int GuestId { get; set; }
        [ForeignKey("GuestId")]
        public Guest? Guest { get; set; }

        public int RoomId { get; set; }
        [ForeignKey("RoomId")]
        public Room? Room { get; set; }

        public decimal AdvanceAmount { get; set; }
        public decimal RentAmount { get; set; }
        public DateTime JoiningDate { get; set; }
        
        public string RentType { get; set; } = "Regular"; // Regular, Daily
        public decimal? PerDayRent { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime RentDueDate { get; set; }

        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid
        public DateTime? LastPaidDate { get; set; }

        public string NoticeStatus { get; set; } = "None"; // None, Pending, Approved
        public bool IsInNoticePeriod { get; set; }
        public DateTime? NoticeStartDate { get; set; }
    }

    public class Complaint
    {
        [Key]
        public int Id { get; set; }
        public int GuestId { get; set; }
        [ForeignKey("GuestId")]
        public Guest? Guest { get; set; }
        public string Type { get; set; } = string.Empty;
        [MaxLength(600)]
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Registered"; // Registered, InProgress, Solved
        public int? EstimatedResolutionDays { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? SolvedDate { get; set; }
        public string? Notes { get; set; }
    }

    public class Transaction
    {
        [Key]
        public int Id { get; set; }
        public int GuestId { get; set; }
        [ForeignKey("GuestId")]
        public Guest? Guest { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string Utr { get; set; } = string.Empty; // UPI Transaction ID (12 digits)
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Success"; // Success, Pending, Rejected
        public string Type { get; set; } = "Rent"; // Rent, Advance, etc.
    }

    public class PropertySetting
    {
        [Key]
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public int PropertyId { get; set; }
        [ForeignKey("PropertyId")]
        public PGProperty? Property { get; set; }
    }
}
