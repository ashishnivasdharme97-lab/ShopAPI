using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;
using ShopAPI.Models;

namespace ShopAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        // User la tyachya notifications
        [HttpGet("my")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var notifications = await _context.Notifications
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Ok(notifications);
        }

        // Notification Read
        [HttpPut("read/{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (notification == null)
            {
                return NotFound(new { message = "Notification not found." });
            }

            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Notification marked as read."
            });
        }
    }
}