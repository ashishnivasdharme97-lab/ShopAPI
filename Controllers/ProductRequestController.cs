
using ShopAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;
using ShopAPI.Models;
using Microsoft.AspNetCore.Mvc;
using ShopAPI.Data;
using ShopAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization; // 👈 ADD THIS



namespace ShopAPI.Controllers
{

     [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class ProductRequestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductRequestController(AppDbContext context)
        {
            _context = context;
        }
[HttpPost]
//[Authorize(Roles = "User")]
public async Task<IActionResult> CreateRequest(ProductRequest request)
{
    request.Status = "Pending";
    request.CreatedAt = DateTime.Now;
     // 👇 user id (JWT madhun yenar)
    //var userId = int.Parse(User.FindFirst("UserId").Value);
    request.UserId = 1;

    _context.ProductRequests.Add(request);
    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "Request sent successfully",
        status = request.Status
    });
}

[HttpGet("admin/requests")]
public async Task<IActionResult> GetAllRequests()
{
    var requests = await _context.ProductRequests
        .Include(x => x.User)
        .Where(x => x.Status == "Pending")
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync();

    return Ok(requests);
}
[HttpGet("admin/history")]
public async Task<IActionResult> RequestHistory()
{
    var requests = await _context.ProductRequests
        .Include(x => x.User)
        .Where(x => x.Status != "Pending")
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync();

    return Ok(requests);
}
[HttpGet("admin/pending")]
public async Task<IActionResult> GetPendingRequests()
{
    var requests = await _context.ProductRequests
        .Include(x => x.User)
        .Where(x => x.Status == "Pending")
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync();

    return Ok(requests);
}
[HttpGet("my")]
//[Authorize(Roles = "User")]
public async Task<IActionResult> MyRequests()
{
    //var userId = int.Parse(User.FindFirst("UserId").Value);
var userId = 1;
    var data = await _context.ProductRequests
        .Where(x => x.UserId == userId)
        .ToListAsync();

    return Ok(data);
}
//[Authorize(Roles = "Admin")]




[HttpPut("approve/{id}")]
//[Authorize(Roles = "Admin")]
public async Task<IActionResult> ApproveRequest(int id)
{
    var request = await _context.ProductRequests
        .FirstOrDefaultAsync(x => x.Id == id);

    if (request == null)
    {
        return NotFound(new { message = "Request not found." });
    }

    if (request.Status == "Approved")
    {
        return BadRequest(new { message = "Request already approved." });
    }

    // Update Status
    request.Status = "Approved";

    // Notification only for requested user
    var notification = new Notification
    {
        UserId = request.UserId,
        Message = $"Your request for '{request.ProductName}' has been approved.",
        CreatedAt = DateTime.Now,
        IsRead = false
    };

    _context.Notifications.Add(notification);

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "Request approved successfully."
    });
}

[HttpPut("reject/{id}")]
//[Authorize(Roles = "Admin")]
public async Task<IActionResult> RejectRequest(int id)
{
    var request = await _context.ProductRequests
        .FirstOrDefaultAsync(x => x.Id == id);

    if (request == null)
    {
        return NotFound(new { message = "Request not found." });
    }

    request.Status = "Rejected";

    var notification = new Notification
    {
        UserId = request.UserId,
        Message = $"Your request for '{request.ProductName}' has been rejected.",
        CreatedAt = DateTime.Now,
        IsRead = false
    };

    _context.Notifications.Add(notification);

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "Request rejected successfully."
    });
}
}}