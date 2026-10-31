using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;
using ShopAPI.Models;
using System.Security.Claims;

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
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new
                {
                    message = "User ID is missing or invalid in the token."
                });
            }

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
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new
                {
                    message = "User ID is missing or invalid in the token."
                });
            }

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

        private bool TryGetUserId(out int userId)
        {
            var userIdClaim =
                User.FindFirst("UserId") ??
                User.FindFirst(ClaimTypes.NameIdentifier) ??
                User.FindFirst("sub");

            return int.TryParse(userIdClaim?.Value, out userId) && userId > 0;
        }
    }
}