
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;
using ShopAPI.Models;

namespace ShopAPI.Controllers
{

     [ApiController]
    [Route("api/[controller]")]
   //[Authorize]
    public class ProductRequestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductRequestController> _logger;

        public ProductRequestController(
            AppDbContext context,
            IWebHostEnvironment environment,
            ILogger<ProductRequestController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }
[HttpPost]
[AllowAnonymous]
public async Task<IActionResult> CreateRequest([FromBody] ProductRequest request)
{
    if (request == null)
    {
        _logger.LogWarning("Product request rejected because the request body was empty.");
        return BadRequest(new
        {
            message = "Invalid request data."
        });
    }

    if (string.IsNullOrWhiteSpace(request.ProductName))
    {
        _logger.LogWarning("Product request rejected because the product name is empty.");
        return BadRequest(new
        {
            message = "Product name is required."
        });
    }

    if (request.Quantity <= 0)
    {
        _logger.LogWarning(
            "Product request rejected for {ProductName} because quantity {Quantity} is invalid.",
            request.ProductName,
            request.Quantity);
        return BadRequest(new
        {
            message = "Valid quantity is required."
        });
    }

    try
    {
        var productRequest = new ProductRequest
        {
            ProductName = request.ProductName,
            Description = request.Description,
            RequestedBy = request.RequestedBy,
            Quantity = request.Quantity,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UserId = null
        };

        _context.ProductRequests.Add(productRequest);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product request {ProductRequestId} for {ProductName} created successfully.",
            productRequest.Id,
            productRequest.ProductName);

        return Ok(new
        {
            message = "Request sent successfully",
            status = productRequest.Status
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "Failed to create a product request for {ProductName}.",
            request.ProductName);

        return StatusCode(500, new
        {
            message = "Request could not be saved.",
        });
    }
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
[AllowAnonymous]
public async Task<IActionResult> MyRequests()
{
    if (!_environment.IsDevelopment())
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "This demo endpoint is available only in Development."
        });
    }

    var data = await _context.ProductRequests
        .Where(x => x.UserId == null || x.UserId == 1)
        .OrderByDescending(x => x.CreatedAt)
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

    if (request.UserId.HasValue)
    {
        var notification = new Notification
        {
            UserId = request.UserId.Value,
            Message = $"Your request for '{request.ProductName}' has been approved.",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Notifications.Add(notification);
    }
    else
    {
        _logger.LogWarning(
            "No approval notification was created for product request {ProductRequestId} because it has no user.",
            request.Id);
    }

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

    if (request.UserId.HasValue)
    {
        var notification = new Notification
        {
            UserId = request.UserId.Value,
            Message = $"Your request for '{request.ProductName}' has been rejected.",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Notifications.Add(notification);
    }
    else
    {
        _logger.LogWarning(
            "No rejection notification was created for product request {ProductRequestId} because it has no user.",
            request.Id);
    }

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "Request rejected successfully."
    });
}
}}