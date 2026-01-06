
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PG_Backend.Data;
using PG_Backend.Models;
using System.Security.Claims;

namespace PG_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplaintsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ComplaintsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Complaint>>> GetComplaints()
        {
            return await _context.Complaints
                .Include(c => c.Guest)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        [HttpGet("my/{userId}")]
        public async Task<ActionResult<IEnumerable<Complaint>>> GetMyComplaints(int userId)
        {
            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.UserId == userId);
            if (guest == null) return NotFound("Guest not found");

            return await _context.Complaints
                .Where(c => c.GuestId == guest.Id)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Complaint>> PostComplaint(Complaint complaint)
        {
            // Ensure guest exists
            var guest = await _context.Guests.FindAsync(complaint.GuestId);
            if (guest == null) return NotFound("Guest not found");

            complaint.Status = "Registered";
            complaint.CreatedDate = DateTime.Now;
            
            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetComplaint", new { id = complaint.Id }, complaint);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Complaint>> GetComplaint(int id)
        {
            var complaint = await _context.Complaints.Include(c => c.Guest).FirstOrDefaultAsync(c => c.Id == id);
            if (complaint == null) return NotFound();
            return complaint;
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] ComplaintUpdateDto update)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            
            if (complaint == null) return NotFound();

            if (complaint.Status == "Solved")
            {
                return BadRequest("Solved complaints cannot be updated.");
            }


            complaint.Status = update.Status;
            if (update.Status == "InProgress")
            {
                complaint.EstimatedResolutionDays = update.EstimatedResolutionDays;
                complaint.Notes = update.Notes;
            }
            else if (update.Status == "Solved")
            {
                complaint.Notes = update.Notes;
                complaint.SolvedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComplaint(int id)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null) return NotFound();

            // Only allow delete if Registered
            if (complaint.Status != "Registered")
            {
                return BadRequest("Only registered complaints can be cancelled.");
            }

            _context.Complaints.Remove(complaint);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class ComplaintUpdateDto
    {
        public string Status { get; set; } = string.Empty;
        public int? EstimatedResolutionDays { get; set; }
        public string? Notes { get; set; }
    }
}
