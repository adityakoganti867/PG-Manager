
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PG_Backend.Data;
using PG_Backend.Models;

namespace PG_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest login)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => (u.Username == login.Username || u.Mobile == login.Username) && u.Password == login.Password);
            if (user == null) return Unauthorized("Invalid Credentials");
            if (!user.IsActive) return Unauthorized("Account Disabled. Contact super admin.");

            string propertyName = "StayNest";
            int propertyId = 0;
            string displayName = user.Username; // Default to Username

            if (user.Role == UserRole.Admin)
            {
                var property = await _context.Properties.FirstOrDefaultAsync(p => p.AdminId == user.Id);
                if (property != null)
                {
                    propertyName = property.Name;
                    propertyId = property.Id;
                }
            }
            else if (user.Role == UserRole.Supervisor)
            {
                var supervisor = await _context.Supervisors.Include(s => s.Property).FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (supervisor != null)
                {
                    displayName = supervisor.Name;
                    if (supervisor.Property != null)
                    {
                        propertyName = supervisor.Property.Name;
                        propertyId = supervisor.PropertyId;
                    }
                }
            }
            else if (user.Role == UserRole.Guest)
            {
                var guest = await _context.Guests.Include(g => g.Property).FirstOrDefaultAsync(g => g.UserId == user.Id);
                if (guest != null)
                {
                    displayName = guest.Name ?? user.Username;
                    if (guest.Property != null)
                    {
                        propertyName = guest.Property.Name;
                        propertyId = guest.PropertyId;
                    }
                }
            }
            else if (user.Role == UserRole.SuperAdmin)
            {
                displayName = "Super Admin";
                propertyName = "Super Portal";
            }

            // Final safety check for displayName
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = !string.IsNullOrEmpty(user.Username) ? user.Username : user.Mobile;
            }

            return Ok(new 
            { 
                id = user.Id, 
                role = (int)user.Role, 
                username = user.Username, 
                mobile = user.Mobile,
                propertyName = propertyName, 
                propertyId = propertyId, 
                displayName = displayName 
            });
        }
        [HttpGet("check-status")]
        public async Task<IActionResult> CheckStatus([FromQuery] string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username || u.Mobile == username);
            if (user == null) return NotFound("User not found");

            // Check if password is not set (null or empty)
            bool isPasswordSet = !string.IsNullOrEmpty(user.Password);
            
            return Ok(new { isPasswordSet });
        }

        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword([FromBody] LoginRequest req)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == req.Username || u.Mobile == req.Username);
            if (user == null) return NotFound("User not found");

            user.Password = req.Password;
            await _context.SaveChangesAsync();

            return Ok("Password updated successfully");
        }
    }

    public class LoginRequest {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
