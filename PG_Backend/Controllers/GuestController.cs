
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
            
            // Logic for Refund amount
            decimal refundAmount = guest.AdvanceAmount - 1000;
            if (refundAmount < 0) refundAmount = 0;

            return Ok(new {
                guest.Id,
                guest.Name,
                guest.RoomNumber,
                guest.AdvanceAmount,
                guest.RentAmount,
                guest.JoiningDate,
                guest.RentDueDate,

                guest.IsInNoticePeriod,
                guest.NoticeStartDate,
                guest.NoticeStatus,

                RefundAmount = refundAmount,

                IsRentDue = DateTime.Now >= guest.RentDueDate,
                guest.PaymentStatus,
                guest.LastPaidDate,

                guest.RentType,
                guest.PerDayRent,
                guest.EndDate,
                User = new { guest.User.Id, guest.User.Mobile, guest.User.IsActive }
            });

        }



        [HttpPost("initiate-notice/{userId}")]
        public async Task<IActionResult> InitiateNotice(int userId)
        {
             var guest = await _context.Guests.FirstOrDefaultAsync(g => g.UserId == userId);
             if (guest == null) return BadRequest("Guest not found");

             if (guest.NoticeStatus == "Pending") return BadRequest("Notice request is already pending.");
             if (guest.NoticeStatus == "Approved") return BadRequest("Already in notice period.");


             // Validation: Must be >= 30 days before Next Due Date (Fixed 30 days rule)
             var today = DateTime.Today;
             var dueDate = guest.RentDueDate.Date;
             var daysUntilDue = (dueDate - today).TotalDays;

             if (daysUntilDue < 30)
             {
                 return BadRequest($"Oops! You have only {daysUntilDue} days left until your next due date. At least 30 days are needed to raise a notice request.");
             }


             guest.NoticeStatus = "Pending";
             // guest.IsInNoticePeriod = false; // Remains false until approved
             // guest.NoticeStartDate = null; // Set on approval
             await _context.SaveChangesAsync();
             
             return Ok("Notice Request Initiated. Waiting for Approval.");
        }


    }
}
