
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PG_Backend.Data;
using PG_Backend.Models;

namespace PG_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GuestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GuestController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("profile/{userId}")]
        public async Task<IActionResult> GetProfile(int userId)
        {
            var guest = await _context.Guests
                .Include(g => g.User)
                .FirstOrDefaultAsync(g => g.UserId == userId);
                
            if (guest == null) return NotFound();

            var stay = await _context.GuestStays
                .FirstOrDefaultAsync(s => s.GuestId == guest.Id);

            if (stay == null) return NotFound("Stay details not found");

            var room = await _context.Rooms.FindAsync(stay.RoomId);

            decimal refundAmount = stay.AdvanceAmount - 1000;
            if (refundAmount < 0) refundAmount = 0;

            return Ok(new {
                guest.Id,
                guest.Name,
                RoomNumber = room?.RoomNumber ?? "N/A",
                stay.AdvanceAmount,
                stay.RentAmount,
                stay.JoiningDate,
                stay.RentDueDate,

                stay.IsInNoticePeriod,
                stay.NoticeStartDate,
                stay.NoticeStatus,

                RefundAmount = refundAmount,

                IsRentDue = DateTime.Now >= stay.RentDueDate,
                stay.PaymentStatus,
                stay.LastPaidDate,

                stay.RentType,
                stay.PerDayRent,
                stay.EndDate,
                User = guest.User != null ? new { guest.User.Id, guest.User.Mobile, guest.User.IsActive } : null
            });
        }

        [HttpPost("initiate-notice/{userId}")]
        public async Task<IActionResult> InitiateNotice(int userId)
        {
             var guest = await _context.Guests.FirstOrDefaultAsync(g => g.UserId == userId);
             if (guest == null) return BadRequest("Guest not found");

             var stay = await _context.GuestStays.FirstOrDefaultAsync(s => s.GuestId == guest.Id);
             if (stay == null) return BadRequest("Stay record not found");

             if (stay.NoticeStatus == "Pending") return BadRequest("Notice request is already pending.");
             if (stay.NoticeStatus == "Approved") return BadRequest("Already in notice period.");

             var today = DateTime.Today;
             var dueDate = stay.RentDueDate.Date;
             var daysUntilDue = (dueDate - today).TotalDays;

             if (daysUntilDue < 30)
             {
                 return BadRequest($"Oops! You have only {daysUntilDue} days left until your next due date. At least 30 days are needed to raise a notice request.");
             }

             stay.NoticeStatus = "Pending";
             await _context.SaveChangesAsync();
             
             return Ok("Notice Request Initiated. Waiting for Approval.");
        }
    }
}
