
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

        [HttpGet("supervisors")]
        public async Task<IActionResult> GetSupervisors()
        {
            var sups = await _context.Supervisors.Include(s => s.User).ToListAsync();
            return Ok(sups);
        }

        [HttpPost("add-supervisor")]
        public async Task<IActionResult> AddSupervisor([FromBody] SupervisorDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Mobile == dto.Mobile))
                return BadRequest("User already exists");

            var user = new User { Mobile = dto.Mobile, Password = "password123", Role = UserRole.Supervisor, IsActive = true };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var supervisor = new Supervisor { UserId = user.Id, Name = dto.Name, JoiningDate = dto.JoiningDate };
            _context.Supervisors.Add(supervisor);
            await _context.SaveChangesAsync();

            return Ok("Supervisor Added");
        }

        [HttpPost("toggle-supervisor/{id}")]
        public async Task<IActionResult> ToggleSupervisor(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { IsActive = user.IsActive });
        }

        [HttpPost("toggle-guest/{id}")]
        public async Task<IActionResult> ToggleGuest(int id)
        {
            var guest = await _context.Guests.FindAsync(id); // Find using GuestId
            if (guest == null) return NotFound();

            var user = await _context.Users.FindAsync(guest.UserId);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { IsActive = user.IsActive });
        }

        [HttpPost("add-guest")]
        public async Task<IActionResult> AddGuest([FromBody] GuestDto dto)
        {
             if (await _context.Users.AnyAsync(u => u.Mobile == dto.Mobile))
                return BadRequest("User already exists");

            var user = new User { Mobile = dto.Mobile, Password = "", Role = UserRole.Guest, IsActive = true };
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
                rentDueDate = joiningDate.AddMonths(1);
            }

            // Check and update Room Availability
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == dto.RoomNumber);
            if (room != null)
            {
                if (room.AvailableBeds <= 0)
                {
                    // Should theoretically be caught by UI, but safe to check
                    return BadRequest("Selected room is fully occupied.");
                }
                room.AvailableBeds -= 1;
                // No need to call Update explicitly, SaveChanges handles tracked entities
            }

            var guest = new Guest { 
                UserId = user.Id, 
                Name = dto.Name, 
                RoomNumber = dto.RoomNumber, 
                Occupation = dto.Occupation,
                AdvanceAmount = dto.AdvanceAmount,
                RentAmount = dto.RentAmount,
                JoiningDate = joiningDate,
                RentDueDate = rentDueDate,
                IsInNoticePeriod = false,
                RentType = dto.RentType,
                PerDayRent = dto.PerDayRent,
                EndDate = dto.EndDate
            };
            _context.Guests.Add(guest);
            await _context.SaveChangesAsync();
            return Ok("Guest Added");
        }

        [HttpGet("guests")]
        public async Task<IActionResult> GetAllGuests()
        {
            var guests = await _context.Guests.Include(g => g.User).ToListAsync();
            return Ok(guests);
        }

        [HttpGet("complaints")]
        public async Task<IActionResult> GetAllComplaints([FromQuery] string? status)
        {
            var query = _context.Complaints.Include(c => c.Guest).AsQueryable();
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }
            return Ok(await query.ToListAsync());
        }

        [HttpGet("rooms")]
        public async Task<IActionResult> GetRooms()
        {
            return Ok(await _context.Rooms.OrderBy(r => r.RoomNumber).ToListAsync());
        }

        [HttpPost("add-room")]
        public async Task<IActionResult> AddRoom([FromBody] RoomDto dto)
        {
            if (await _context.Rooms.AnyAsync(r => r.RoomNumber == dto.RoomNumber))
                return BadRequest("Room Number already exists");

            var room = new Room
            {
                RoomNumber = dto.RoomNumber,
                FloorNumber = dto.FloorNumber,
                SharingType = dto.SharingType,
                RoomType = dto.RoomType,
                TotalBeds = dto.SharingType,
                AvailableBeds = dto.SharingType
            };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return Ok("Room Added");
        }

        [HttpGet("available-rooms")]
        public async Task<IActionResult> GetAvailableRooms([FromQuery] int share, [FromQuery] int? floor, [FromQuery] string? roomType)
        {
            Console.WriteLine($"GetAvailableRooms called with: share={share}, floor={floor}, roomType='{roomType}'");
            
            // Build query with all conditions at once
            var results = await _context.Rooms
                .Where(r => r.AvailableBeds > 0 
                    && r.SharingType == share
                    && (!floor.HasValue || r.FloorNumber == floor.Value)
                    && (string.IsNullOrEmpty(roomType) || r.RoomType == roomType))
                .ToListAsync();
            
            Console.WriteLine($"Query returned {results.Count} rooms:");
            foreach (var room in results)
            {
                Console.WriteLine($"  Room: {room.RoomNumber}, Type: '{room.RoomType}', Floor: {room.FloorNumber}, Share: {room.SharingType}, Available: {room.AvailableBeds}");
            }
            
            return Ok(results);
        }

        [HttpPost("approve-notice/{guestId}")]
        public async Task<IActionResult> ApproveNotice(int guestId)
        {
            var guest = await _context.Guests.FindAsync(guestId);
            if (guest == null) return NotFound("Guest not found");

            if (guest.NoticeStatus != "Pending") return BadRequest("No pending notice request found.");

            guest.NoticeStatus = "Approved";
            guest.IsInNoticePeriod = true;
            guest.NoticeStartDate = DateTime.Now;
            
            await _context.SaveChangesAsync();
            return Ok("Notice Period Approved");
        }

        [HttpPost("revert-notice/{guestId}")]
        public async Task<IActionResult> RevertNotice(int guestId)
        {
            var guest = await _context.Guests.FindAsync(guestId);
            if (guest == null) return NotFound("Guest not found");

            if (!guest.IsInNoticePeriod) return BadRequest("Guest is not in notice period.");

            guest.NoticeStatus = "None";
            guest.IsInNoticePeriod = false;
            guest.NoticeStartDate = null;
            // Note: RentDueDate is left as is. It was likely set when notice was approved or before.
            // If the user wants to manually adjust it, they can do so (if we build that feature), 
            // but for now, reverting simply removes the notice status.
            
            await _context.SaveChangesAsync();
            return Ok("Notice Period Reverted");
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
        
        // New Props
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
