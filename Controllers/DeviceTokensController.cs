using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;
using ShopAPI.DTOs;
using ShopAPI.Models;

namespace ShopAPI.Controllers
{
    [ApiController]
    [Route("api/device-tokens")]
    [Authorize(Roles = "User,Admin")]
    public class DeviceTokensController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DeviceTokensController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] DeviceTokenRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request.Token.Any(char.IsWhiteSpace) || request.Token.Any(char.IsControl))
            {
                return BadRequest(new { message = "The FCM token format is invalid." });
            }

            if (!TryGetUserId(out var userId) ||
                !await _context.Users.AnyAsync(user => user.Id == userId, cancellationToken))
            {
                return Unauthorized(new { message = "A valid authenticated user is required." });
            }

            var token = request.Token.Trim();
            var tokenHash = HashToken(token);
            var deviceToken = await _context.DeviceTokens
                .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

            if (deviceToken is null)
            {
                _context.DeviceTokens.Add(new DeviceToken
                {
                    UserId = userId,
                    Token = token,
                    TokenHash = tokenHash,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                deviceToken.UserId = userId;
                deviceToken.Token = token;
                deviceToken.IsActive = true;
                deviceToken.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "FCM token registered." });
        }

        [HttpDelete("unregister")]
        public async Task<IActionResult> Unregister(
            [FromBody] DeviceTokenRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request.Token.Any(char.IsWhiteSpace) || request.Token.Any(char.IsControl))
            {
                return BadRequest(new { message = "The FCM token format is invalid." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "A valid authenticated user is required." });
            }

            var tokenHash = HashToken(request.Token.Trim());
            var deviceToken = await _context.DeviceTokens
                .FirstOrDefaultAsync(item =>
                    item.TokenHash == tokenHash && item.UserId == userId,
                    cancellationToken);

            if (deviceToken is null)
            {
                return NoContent();
            }

            deviceToken.IsActive = false;
            deviceToken.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        private bool TryGetUserId(out int userId)
        {
            var userIdClaim =
                User.FindFirst("UserId") ??
                User.FindFirst(ClaimTypes.NameIdentifier) ??
                User.FindFirst("sub");

            return int.TryParse(userIdClaim?.Value, out userId) && userId > 0;
        }

        private static string HashToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
    }
}