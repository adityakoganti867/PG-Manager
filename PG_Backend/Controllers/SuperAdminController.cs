
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PG_Backend.Data;
using PG_Backend.Models;

namespace PG_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuperAdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SuperAdminController(AppDbContext context)
        {
            _context = context;
        }

        private int CurrentSuperAdminId => int.TryParse(Request.Headers["X-Admin-Id"], out var id) ? id : 0;

        [HttpPost("add-property")]
        public async Task<IActionResult> AddProperty([FromBody] AddPropertyRequest req)
        {
            // Verify SuperAdmin
            var superAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Id == CurrentSuperAdminId && u.Role == UserRole.SuperAdmin);
            if (superAdmin == null) return Unauthorized("Only Super Admin can perform this action");

            // Create Admin User
            var adminUser = new User
            {
                Username = req.AdminUsername,
                Mobile = req.AdminMobile,
                Password = "", // Password will be set on first login
                Role = UserRole.Admin,
                IsActive = true
            };
            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            // Create Property
            var property = new PGProperty
            {
                Name = req.PropertyName,
                Address = req.Address,
                OwnerName = req.OwnerName,
                AdminId = adminUser.Id
            };
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            // Initialize default settings for the property
            _context.PropertySettings.AddRange(new List<PropertySetting>
            {
                new PropertySetting { PropertyId = property.Id, Key = "UpiId", Value = "" },
                new PropertySetting { PropertyId = property.Id, Key = "UpiName", Value = "" }
            });
            await _context.SaveChangesAsync();

            return Ok(new { PropertyId = property.Id, AdminId = adminUser.Id });
        }

        [HttpGet("properties")]
        public async Task<IActionResult> GetAllProperties()
        {
            var superAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Id == CurrentSuperAdminId && u.Role == UserRole.SuperAdmin);
            if (superAdmin == null) return Unauthorized();

            var properties = await _context.Properties
                .Include(p => p.Admin)
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    address = p.Address,
                    ownerName = p.OwnerName,
                    adminUsername = p.Admin != null ? p.Admin.Username : "",
                    adminMobile = p.Admin != null ? p.Admin.Mobile : "",
                    isActive = p.Admin != null ? p.Admin.IsActive : false,
                    adminId = p.AdminId
                })
                .ToListAsync();

            return Ok(properties);
        }

        [HttpPost("toggle-property/{id}")]
        public async Task<IActionResult> TogglePropertyStatus(int id)
        {
            var superAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Id == CurrentSuperAdminId && u.Role == UserRole.SuperAdmin);
            if (superAdmin == null) return Unauthorized();

            var property = await _context.Properties
                .Include(p => p.Admin)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null || property.Admin == null) return NotFound("Property or Admin not found");

            bool newStatus = !property.Admin.IsActive;

            // 1. Toggle Admin
            property.Admin.IsActive = newStatus;
            _context.Entry(property.Admin).State = EntityState.Modified;

            // 2. Toggle all Supervisors of this property
            var supervisorUsers = await _context.Supervisors
                .Where(s => s.PropertyId == id && s.User != null)
                .Select(s => s.User!)
                .ToListAsync();

            foreach (var u in supervisorUsers)
            {
                u.IsActive = newStatus;
                _context.Entry(u).State = EntityState.Modified;
            }

            // 3. Toggle all Guests of this property
            var guestUsers = await _context.Guests
                .Where(g => g.PropertyId == id && g.User != null)
                .Select(g => g.User!)
                .ToListAsync();

            foreach (var u in guestUsers)
            {
                u.IsActive = newStatus;
                _context.Entry(u).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
            return Ok(new { status = newStatus });
        }
    }

    public class AddPropertyRequest
    {
        public string PropertyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string AdminUsername { get; set; } = string.Empty;
        public string AdminMobile { get; set; } = string.Empty;
    }
}
