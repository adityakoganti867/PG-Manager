
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PG_Backend.Models;
using Razorpay.Api;

namespace PG_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {

        private const string KEY_ID = "rzp_test_S0VAf9TqG9Ylvv"; 
        private const string KEY_SECRET = "mI0dKFBtsw3rDkw1LfcML9NM"; 


        private readonly PG_Backend.Data.AppDbContext _context;

        public PaymentController(PG_Backend.Data.AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("create-order")]
        public IActionResult CreateOrder([FromBody] PaymentRequest request)
        {
            try
            {
                Dictionary<string, object> input = new Dictionary<string, object>();
                input.Add("amount", request.Amount * 100); // Amount in paise
                input.Add("currency", "INR");
                input.Add("receipt", "receipt_" + Guid.NewGuid().ToString().Substring(0,8));
                input.Add("payment_capture", 1);

                RazorpayClient client = new RazorpayClient(KEY_ID, KEY_SECRET);
                Order order = client.Order.Create(input);
                return Ok(new { orderId = order["id"].ToString() });
            }

            catch (Exception ex)
            {
                return BadRequest("Error creating order: " + ex.Message);
            }
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyPayment([FromBody] PaymentVerificationRequest request)
        {
            // Verify signature using RazorpayUtils or manual hash
            // For now, accept all as success for dev
            
            var guest = await _context.Guests.FindAsync(request.GuestId);
            if (guest == null) return NotFound("Guest not found");

            guest.PaymentStatus = "Paid";
            guest.LastPaidDate = DateTime.Now;

            // Logic for Monthly Rent Update
            if (guest.RentType == "Regular") 
            {
                guest.RentDueDate = guest.RentDueDate.AddDays(30);
            }
            
            // Record Transaction
            var transaction = new Transaction
            {
                GuestId = guest.Id,
                OrderId = request.OrderId,
                PaymentId = request.PaymentId,
                Amount = guest.RentAmount, // Assuming full rent payment for now
                PaymentDate = DateTime.Now,
                Status = "Success",
                Type = "Rent"
            };
            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            return Ok(new { status = "success", nextDueDate = guest.RentDueDate, paymentStatus = "Paid" });
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
            var history = await _context.Transactions
                                        .Include(t => t.Guest)
                                        .OrderByDescending(t => t.PaymentDate)
                                        .Select(t => new 
                                        {
                                            t.Id,
                                            t.OrderId,
                                            t.PaymentId,
                                            t.Amount,
                                            t.PaymentDate,
                                            t.Status,
                                            t.Type,
                                            GuestName = t.Guest != null ? t.Guest.Name : "Unknown",
                                            RoomNumber = t.Guest != null ? t.Guest.RoomNumber : "N/A"
                                        })
                                        .ToListAsync();
            return Ok(history);
        }

    }

    public class PaymentRequest
    {
        public decimal Amount { get; set; }
    }


    public class PaymentVerificationRequest
    {
        public int GuestId { get; set; }
        public string OrderId { get; set; } = string.Empty;

        public string PaymentId { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }
}
