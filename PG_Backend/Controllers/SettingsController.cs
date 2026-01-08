
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PG_Backend.Data;
using PG_Backend.Models;

namespace PG_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        private int CurrentAdminId => int.TryParse(Request.Headers["X-Admin-Id"], out var id) ? id : 0;

        private async Task<int> GetPropertyId()
        {
            var adminId = CurrentAdminId;
            var property = await _context.Properties.FirstOrDefaultAsync(p => p.AdminId == adminId);
            return property?.Id ?? 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var propertyId = await GetPropertyId();
            var settings = await _context.PropertySettings.Where(s => s.PropertyId == propertyId).ToListAsync();
            return Ok(settings);
        }

        [HttpPost("update-upi")]
        public async Task<IActionResult> UpdateUpi([FromBody] UpiSettingsDto dto)
        {
            var propertyId = await GetPropertyId();
            if (propertyId == 0) return Unauthorized("Property not found for this admin");

            var upiId = await _context.PropertySettings.FirstOrDefaultAsync(s => s.Key == "UpiId" && s.PropertyId == propertyId);
            var upiName = await _context.PropertySettings.FirstOrDefaultAsync(s => s.Key == "UpiName" && s.PropertyId == propertyId);

            if (upiId == null) {
                upiId = new PropertySetting { Key = "UpiId", Value = dto.UpiId, PropertyId = propertyId };
                _context.PropertySettings.Add(upiId);
            } else {
                upiId.Value = dto.UpiId;
            }

            if (upiName == null) {
                upiName = new PropertySetting { Key = "UpiName", Value = dto.UpiName, PropertyId = propertyId };
                _context.PropertySettings.Add(upiName);
            } else {
                upiName.Value = dto.UpiName;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "UPI Settings Updated Successfully" });
        }
    }

    public class UpiSettingsDto
    {
        public string UpiId { get; set; } = string.Empty;
        public string UpiName { get; set; } = string.Empty;
    }
}
