
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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Mobile == login.Mobile && u.Password == login.Password);
            if (user == null) return Unauthorized("Invalid Credentials");
            if (!user.IsActive) return Unauthorized("Account Disabled");

            return Ok(new { user.Id, user.Role, user.Mobile });
        }
        [HttpGet("check-status")]
        public async Task<IActionResult> CheckStatus([FromQuery] string mobile)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Mobile == mobile);
            if (user == null) return NotFound("User not found");

            // Check if password is not set (null or empty)
            bool isPasswordSet = !string.IsNullOrEmpty(user.Password);
            
            return Ok(new { isPasswordSet });
        }

        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword([FromBody] LoginRequest req)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Mobile == req.Mobile);
            if (user == null) return NotFound("User not found");

            user.Password = req.Password;
            await _context.SaveChangesAsync();

            return Ok("Password updated successfully");
        }
    }

    public class LoginRequest {
        public string Mobile { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
