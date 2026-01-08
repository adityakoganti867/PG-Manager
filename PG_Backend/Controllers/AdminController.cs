
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PG_Backend.Data;
using PG_Backend.Models;

namespace PG_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        private int CurrentAdminId => int.TryParse(Request.Headers["X-Admin-Id"], out var id) ? id : 0;

        // Helper to get the default property for the current admin
        private async Task<int> GetPropertyId()
        {
            var adminId = CurrentAdminId;
            var property = await _context.Properties.FirstOrDefaultAsync(p => p.AdminId == adminId);
            return property?.Id ?? 0;
        }

        [HttpGet("supervisors")]
        public async Task<IActionResult> GetSupervisors()
        {
            var propertyId = await GetPropertyId();
            var sups = await _context.Supervisors
                .Where(s => s.PropertyId == propertyId)
                .Include(s => s.User)
                .ToListAsync();
            return Ok(sups);
        }

        [HttpPost("add-supervisor")]
        public async Task<IActionResult> AddSupervisor([FromBody] SupervisorDto dto)
        {
            var propertyId = await GetPropertyId();
            if (propertyId == 0) return BadRequest("No property found for this admin");

            if (await _context.Users.AnyAsync(u => u.Mobile == dto.Mobile.Trim()))
                return BadRequest("User already exists");

            var user = new User { 
                Username = dto.Mobile.Trim(),
                Mobile = dto.Mobile.Trim(), 
                Password = "", 
                Role = UserRole.Supervisor, 
                IsActive = true 
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var supervisor = new Supervisor { UserId = user.Id, Name = dto.Name, JoiningDate = dto.JoiningDate, PropertyId = propertyId };
            _context.Supervisors.Add(supervisor);
            await _context.SaveChangesAsync();

            return Ok("Supervisor Added");
        }

        [HttpPost("toggle-supervisor/{id}")]
        public async Task<IActionResult> ToggleSupervisor(int id)
        {
            var propertyId = await GetPropertyId();
            var supervisor = await _context.Supervisors.FirstOrDefaultAsync(s => s.UserId == id && s.PropertyId == propertyId);
            if (supervisor == null) return Unauthorized("Unauthorized access to supervisor");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { IsActive = user.IsActive });
        }

        [HttpPost("toggle-guest/{id}")]
        public async Task<IActionResult> ToggleGuest(int id)
        {
            var propertyId = await GetPropertyId();
            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.Id == id && g.PropertyId == propertyId);
            if (guest == null) return Unauthorized("Unauthorized access to guest");

            var user = await _context.Users.FindAsync(guest.UserId);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { IsActive = user.IsActive });
        }

        [HttpPost("add-guest")]
        public async Task<IActionResult> AddGuest([FromBody] GuestDto dto)
        {
            var propertyId = await GetPropertyId();
            if (propertyId == 0) return BadRequest("No property found for this admin");

            if (await _context.Users.AnyAsync(u => u.Mobile == dto.Mobile.Trim()))
                return BadRequest("User already exists");

            var user = new User { 
                Username = dto.Mobile.Trim(),
                Mobile = dto.Mobile.Trim(), 
                Password = "", 
                Role = UserRole.Guest, 
                IsActive = true 
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Calculate Rent Due Date
            var joiningDate = dto.JoiningDate != default ? dto.JoiningDate : DateTime.Now; 
            DateTime rentDueDate;

            if (dto.RentType == "Daily")
            {
                rentDueDate = dto.EndDate ?? joiningDate;
            }
            else
            {
                rentDueDate = joiningDate.AddDays(30);
            }

            // Check and update Room Availability
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == dto.RoomNumber && r.PropertyId == propertyId);
            if (room == null) return NotFound("Room not found");
            
            if (room.AvailableBeds <= 0)
            {
                return BadRequest("Selected room is fully occupied.");
            }
            room.AvailableBeds -= 1;

            var guest = new Guest { 
                UserId = user.Id, 
                Name = dto.Name, 
                Occupation = dto.Occupation,
                PropertyId = propertyId
            };
            _context.Guests.Add(guest);
            await _context.SaveChangesAsync();

            var stay = new GuestStay {
                GuestId = guest.Id,
                RoomId = room.Id,
                AdvanceAmount = dto.AdvanceAmount,
                RentAmount = dto.RentAmount,
                JoiningDate = joiningDate,
                RentDueDate = rentDueDate,
                RentType = dto.RentType,
                PerDayRent = dto.PerDayRent,
                EndDate = dto.EndDate
            };
            _context.GuestStays.Add(stay);
            await _context.SaveChangesAsync();

            return Ok("Guest Added");
        }

        [HttpGet("guests")]
        public async Task<IActionResult> GetAllGuests()
        {
            var propertyId = await GetPropertyId();
            var guests = await _context.Guests
                .Where(g => g.PropertyId == propertyId)
                .Include(g => g.User)
                .Select(g => new {
                    g.Id,
                    g.Name,
                    g.Occupation,
                    User = g.User,
                    Stay = _context.GuestStays.FirstOrDefault(s => s.GuestId == g.Id)
                })
                .ToListAsync();

            // Map to a friendlier frontend format
            var results = guests.Select(g => new {
                g.Id,
                g.Name,
                g.Occupation,
                g.User,
                RoomNumber = _context.Rooms.Where(r => r.Id == g.Stay.RoomId).Select(r => r.RoomNumber).FirstOrDefault(),
                AdvanceAmount = g.Stay?.AdvanceAmount ?? 0,
                RentAmount = g.Stay?.RentAmount ?? 0,
                JoiningDate = g.Stay?.JoiningDate,
                RentDueDate = g.Stay?.RentDueDate,
                NoticeStatus = g.Stay?.NoticeStatus ?? "None",
                IsInNoticePeriod = g.Stay?.IsInNoticePeriod ?? false,
                PaymentStatus = g.Stay?.PaymentStatus ?? "Pending"
            });

            return Ok(results);
        }

        [HttpGet("complaints")]
        public async Task<IActionResult> GetAllComplaints([FromQuery] string? status)
        {
            var propertyId = await GetPropertyId();
            var query = _context.Complaints
                .Include(c => c.Guest)
                .Where(c => c.Guest.PropertyId == propertyId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }
            return Ok(await query.ToListAsync());
        }

        [HttpGet("rooms")]
        public async Task<IActionResult> GetRooms()
        {
            var propertyId = await GetPropertyId();
            return Ok(await _context.Rooms
                .Where(r => r.PropertyId == propertyId)
                .OrderBy(r => r.RoomNumber)
                .ToListAsync());
        }

        [HttpPost("add-room")]
        public async Task<IActionResult> AddRoom([FromBody] RoomDto dto)
        {
            var propertyId = await GetPropertyId();
            if (propertyId == 0) return BadRequest("No property found for this admin");

            if (await _context.Rooms.AnyAsync(r => r.RoomNumber == dto.RoomNumber && r.PropertyId == propertyId))
                return BadRequest("Room Number already exists for this PG");

            var room = new Room
            {
                RoomNumber = dto.RoomNumber,
                FloorNumber = dto.FloorNumber,
                SharingType = dto.SharingType,
                RoomType = dto.RoomType,
                TotalBeds = dto.SharingType,
                AvailableBeds = dto.SharingType,
                PropertyId = propertyId
            };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return Ok("Room Added");
        }

        [HttpGet("available-rooms")]
        public async Task<IActionResult> GetAvailableRooms([FromQuery] int share, [FromQuery] int? floor, [FromQuery] string? roomType)
        {
            var propertyId = await GetPropertyId();
            var results = await _context.Rooms
                .Where(r => r.PropertyId == propertyId 
                    && r.AvailableBeds > 0 
                    && r.SharingType == share
                    && (!floor.HasValue || r.FloorNumber == floor.Value)
                    && (string.IsNullOrEmpty(roomType) || r.RoomType == roomType))
                .ToListAsync();
            
            return Ok(results);
        }

        [HttpPost("approve-notice/{guestId}")]
        public async Task<IActionResult> ApproveNotice(int guestId)
        {
            var propertyId = await GetPropertyId();
            var stay = await _context.GuestStays
                .Include(s => s.Guest)
                .FirstOrDefaultAsync(s => s.GuestId == guestId && s.Guest.PropertyId == propertyId);

            if (stay == null) return NotFound("Stay record not found or unauthorized");

            if (stay.NoticeStatus != "Pending") return BadRequest("No pending notice request found.");

            stay.NoticeStatus = "Approved";
            stay.IsInNoticePeriod = true;
            stay.NoticeStartDate = DateTime.Now;
            
            await _context.SaveChangesAsync();
            return Ok("Notice Period Approved");
        }

        [HttpPost("revert-notice/{guestId}")]
        public async Task<IActionResult> RevertNotice(int guestId)
        {
            var propertyId = await GetPropertyId();
            var stay = await _context.GuestStays
                .Include(s => s.Guest)
                .FirstOrDefaultAsync(s => s.GuestId == guestId && s.Guest.PropertyId == propertyId);

            if (stay == null) return NotFound("Stay record not found or unauthorized");

            if (!stay.IsInNoticePeriod) return BadRequest("Guest is not in notice period.");

            stay.NoticeStatus = "None";
            stay.IsInNoticePeriod = false;
            stay.NoticeStartDate = null;
            
            await _context.SaveChangesAsync();
            return Ok("Notice Period Reverted");
        }

        [HttpPost("reset-password/{userId}")]
        public async Task<IActionResult> ResetPassword(int userId)
        {
            var propertyId = await GetPropertyId();
            bool isOwned = await _context.Guests.AnyAsync(g => g.UserId == userId && g.PropertyId == propertyId) ||
                           await _context.Supervisors.AnyAsync(s => s.UserId == userId && s.PropertyId == propertyId);

            if (!isOwned) return Unauthorized("Unauthorized to reset password for this user");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            user.Password = ""; // Clear password to force reset on next login
            await _context.SaveChangesAsync();
            return Ok("Password Reset Successfully");
        }
    }

    public class SupervisorDto {
        public string Name { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; }
    }

    public class GuestDto {
        public string Name { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string Occupation { get; set; } = string.Empty;
        public decimal AdvanceAmount { get; set; }
        public decimal RentAmount { get; set; }
        public DateTime JoiningDate { get; set; }
        
        public string RentType { get; set; } = "Regular";
        public decimal? PerDayRent { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class RoomDto {
        public string RoomNumber { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public int SharingType { get; set; }
        public string RoomType { get; set; } = string.Empty;
    }
}
