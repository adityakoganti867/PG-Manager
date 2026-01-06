
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PG_Backend.Models
{
    public enum UserRole
    {
        Admin,
        Supervisor,
        Guest
    }

    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Mobile { get; set; } = string.Empty; // Used as Login ID
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
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
    }

    public class Guest
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string Occupation { get; set; } = string.Empty;
        public decimal AdvanceAmount { get; set; }
        public decimal RentAmount { get; set; }
        public DateTime JoiningDate { get; set; }

        public string RentType { get; set; } = "Regular";
        public decimal? PerDayRent { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime RentDueDate { get; set; }
        public bool IsInNoticePeriod { get; set; }

        public DateTime? NoticeStartDate { get; set; }

        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid
        public DateTime? LastPaidDate { get; set; }

        public string NoticeStatus { get; set; } = "None"; // None, Pending, Approved
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
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Success";
        public string Type { get; set; } = "Rent"; // Rent, Advance, etc.
    }
}
