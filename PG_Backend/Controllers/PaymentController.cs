
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PG_Backend.Models;
using PG_Backend.Data;

namespace PG_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentController(AppDbContext context)
        {
            _context = context;
        }

        private int CurrentAdminId => int.TryParse(Request.Headers["X-Admin-Id"], out var id) ? id : 0;

        [HttpPost("create-order-for-guest")]
        public async Task<IActionResult> CreateOrderForGuest([FromBody] PaymentRequestForGuest request)
        {
            var guest = await _context.Guests.FindAsync(request.GuestId);
            if (guest == null) return NotFound("Guest not found");

            var upiIdSetting = await _context.PropertySettings.FirstOrDefaultAsync(s => s.Key == "UpiId" && s.PropertyId == guest.PropertyId);
            var upiNameSetting = await _context.PropertySettings.FirstOrDefaultAsync(s => s.Key == "UpiName" && s.PropertyId == guest.PropertyId);

            string upiId = upiIdSetting?.Value ?? "123456789@ybl";
            string upiName = upiNameSetting?.Value ?? "Ramesh Kumar";

            string upiUrl = $"upi://pay?pa={upiId}&pn={Uri.EscapeDataString(upiName)}&am={request.Amount}&cu=INR";
            return Ok(new { upiUrl });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyPayment([FromBody] PaymentVerificationRequest request)
        {
            var guest = await _context.Guests.FindAsync(request.GuestId);
            if (guest == null) return NotFound("Guest not found");

            var stay = await _context.GuestStays.FirstOrDefaultAsync(s => s.GuestId == guest.Id);
            if (stay != null) {
                stay.PaymentStatus = "Pending";
            }
            
            var transaction = new Transaction
            {
                GuestId = guest.Id,
                Utr = request.Utr,
                Amount = request.Amount,
                PaymentDate = DateTime.Now,
                Status = "Pending",
                Type = "Rent"
            };
            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            return Ok(new { status = "submitted", paymentStatus = "Pending" });
        }

        [HttpPost("approve/{transactionId}")]
        public async Task<IActionResult> ApprovePayment(int transactionId)
        {
            var adminId = CurrentAdminId;
            var transaction = await _context.Transactions
                .Include(t => t.Guest)
                .ThenInclude(g => g.Property)
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.Guest.Property.AdminId == adminId);

            if (transaction == null) return NotFound("Transaction not found or unauthorized");

            transaction.Status = "Success";
            
            var stay = await _context.GuestStays.FirstOrDefaultAsync(s => s.GuestId == transaction.GuestId);
            if (stay != null)
            {
                stay.PaymentStatus = "Paid";
                stay.LastPaidDate = DateTime.Now;

                if (transaction.Type == "Rent" && stay.RentType == "Regular")
                {
                    stay.RentDueDate = stay.RentDueDate.AddDays(30);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { status = "success" });
        }

        [HttpPost("reject/{transactionId}")]
        public async Task<IActionResult> RejectPayment(int transactionId)
        {
            var adminId = CurrentAdminId;
            var transaction = await _context.Transactions
                .Include(t => t.Guest)
                .ThenInclude(g => g.Property)
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.Guest.Property.AdminId == adminId);

            if (transaction == null) return NotFound("Transaction not found or unauthorized");

            transaction.Status = "Rejected";

            var stay = await _context.GuestStays.FirstOrDefaultAsync(s => s.GuestId == transaction.GuestId);
            if (stay != null)
            {
                stay.PaymentStatus = "Pending";
            }

            await _context.SaveChangesAsync();
            return Ok(new { status = "rejected" });
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingPayments()
        {
            var adminId = CurrentAdminId;
            var pending = await _context.Transactions
                .Include(t => t.Guest)
                .ThenInclude(g => g.Property)
                .Where(t => t.Status == "Pending" && t.Guest.Property.AdminId == adminId)
                .OrderByDescending(t => t.PaymentDate)
                .Select(t => new 
                {
                    t.Id,
                    t.Utr,
                    t.Amount,
                    t.PaymentDate,
                    t.Status,
                    GuestName = t.Guest != null ? t.Guest.Name : "Unknown",
                    RoomNumber = _context.Rooms.Where(r => r.Id == _context.GuestStays.Where(s => s.GuestId == t.GuestId).Select(s => s.RoomId).FirstOrDefault()).Select(r => r.RoomNumber).FirstOrDefault()
                })
                .ToListAsync();
            return Ok(pending);
        }

        [HttpGet("history/{guestId}")]
        public async Task<IActionResult> GetMyHistory(int guestId)
        {
             var history = await _context.Transactions
                                         .Where(t => t.GuestId == guestId)
                                         .OrderByDescending(t => t.PaymentDate)
                                         .ToListAsync();
             return Ok(history);
        }

        [HttpGet("history/all")]
        public async Task<IActionResult> GetAllHistory()
        {
            var adminId = CurrentAdminId;
            var history = await _context.Transactions
                                        .Include(t => t.Guest)
                                        .ThenInclude(g => g.Property)
                                        .Where(t => t.Guest.Property.AdminId == adminId)
                                        .OrderByDescending(t => t.PaymentDate)
                                        .Select(t => new 
                                        {
                                            t.Id,
                                            t.Utr,
                                            t.Amount,
                                            t.PaymentDate,
                                            t.Status,
                                            t.Type,
                                            GuestName = t.Guest != null ? t.Guest.Name : "Unknown",
                                            RoomNumber = _context.Rooms.Where(r => r.Id == _context.GuestStays.Where(s => s.GuestId == t.GuestId).Select(s => s.RoomId).FirstOrDefault()).Select(r => r.RoomNumber).FirstOrDefault()
                                        })
                                        .ToListAsync();
            return Ok(history);
        }

    }

    public class PaymentRequestForGuest
    {
        public int GuestId { get; set; }
        public decimal Amount { get; set; }
    }

    public class PaymentVerificationRequest
    {
        public int GuestId { get; set; }
        public string Utr { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
