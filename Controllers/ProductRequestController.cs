
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;
using ShopAPI.Models;
using ShopAPI.Services;
using System.Security.Claims;
using Npgsql;

namespace ShopAPI.Controllers
{

     [ApiController]
    [Route("api/[controller]")]
   //[Authorize]
    public class ProductRequestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductRequestController> _logger;
        private readonly NotificationEventService _notificationEvents;

        public ProductRequestController(
            AppDbContext context,
            ILogger<ProductRequestController> logger,
            NotificationEventService notificationEvents)
        {
            _context = context;
            _logger = logger;
            _notificationEvents = notificationEvents;
        }
[HttpPost]
[AllowAnonymous]
public async Task<IActionResult> CreateRequest(
    [FromBody] ProductRequest request,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
    CancellationToken cancellationToken)
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

    if (idempotencyKey is not null &&
        (idempotencyKey.Length > 128 || idempotencyKey.Any(char.IsControl)))
    {
        return BadRequest(new { message = "Idempotency-Key must be at most 128 characters." });
    }

    idempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
        ? null
        : idempotencyKey.Trim();

    if (idempotencyKey is not null)
    {
        var existingRequest = await _context.ProductRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(
                productRequest => productRequest.IdempotencyKey == idempotencyKey,
                cancellationToken);

        if (existingRequest is not null)
        {
            return Ok(new
            {
                message = "Request already received.",
                requestId = existingRequest.Id,
                status = existingRequest.Status
            });
        }
    }

    int? authenticatedUserId = null;
    if (User.Identity?.IsAuthenticated == true)
    {
        if (!TryGetUserId(out var claimUserId) ||
            !await _context.Users.AnyAsync(user => user.Id == claimUserId, cancellationToken))
        {
            return Unauthorized(new { message = "A valid authenticated user is required." });
        }

        authenticatedUserId = claimUserId;
    }

    try
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        var productRequest = new ProductRequest
        {
            ProductName = request.ProductName,
            Description = request.Description,
            RequestedBy = request.RequestedBy,
            Quantity = request.Quantity,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UserId = authenticatedUserId,
            IdempotencyKey = idempotencyKey
        };

        _context.ProductRequests.Add(productRequest);
        await _context.SaveChangesAsync(cancellationToken);

        await _notificationEvents.CreateBroadcastAsync(
            $"New product request received: {productRequest.ProductName}.",
            $"product-request:{productRequest.Id}:created:broadcast",
            NotificationEventService.BroadcastTopic,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Product request {ProductRequestId} for {ProductName} created successfully.",
            productRequest.Id,
            productRequest.ProductName);

        return Ok(new
        {
            message = "Request sent successfully",
            requestId = productRequest.Id,
            status = productRequest.Status
        });
    }
    catch (DbUpdateException exception)
        when (idempotencyKey is not null &&
              exception.InnerException is PostgresException
              {
                  SqlState: PostgresErrorCodes.UniqueViolation,
                  ConstraintName: "IX_ProductRequests_IdempotencyKey"
              })
    {
        var existingRequest = await _context.ProductRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(
                productRequest => productRequest.IdempotencyKey == idempotencyKey,
                cancellationToken);

        if (existingRequest is not null)
        {
            return Ok(new
            {
                message = "Request already received.",
                requestId = existingRequest.Id,
                status = existingRequest.Status
            });
        }

        throw;
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
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetAllRequests()
{
    var requests = await _context.ProductRequests
        .Include(x => x.User)
        .Where(x => x.Status == "Pending")
        .OrderByDescending(x => x.CreatedAt)
        .Select(x => new
        {
            x.Id,
            x.ProductName,
            x.Description,
            x.RequestedBy,
            x.Quantity,
            x.Status,
            x.UserId,
            x.CreatedAt,
            User = x.User == null ? null : new { x.User.Id, x.User.Name, x.User.Email }
        })
        .ToListAsync();

    return Ok(requests);
}
[HttpGet("admin/history")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> RequestHistory()
{
    var requests = await _context.ProductRequests
        .Include(x => x.User)
        .Where(x => x.Status != "Pending")
        .OrderByDescending(x => x.CreatedAt)
        .Select(x => new
        {
            x.Id,
            x.ProductName,
            x.Description,
            x.RequestedBy,
            x.Quantity,
            x.Status,
            x.UserId,
            x.CreatedAt,
            User = x.User == null ? null : new { x.User.Id, x.User.Name, x.User.Email }
        })
        .ToListAsync();

    return Ok(requests);
}
[HttpGet("admin/pending")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetPendingRequests()
{
    var requests = await _context.ProductRequests
        .Include(x => x.User)
        .Where(x => x.Status == "Pending")
        .OrderByDescending(x => x.CreatedAt)
        .Select(x => new
        {
            x.Id,
            x.ProductName,
            x.Description,
            x.RequestedBy,
            x.Quantity,
            x.Status,
            x.UserId,
            x.CreatedAt,
            User = x.User == null ? null : new { x.User.Id, x.User.Name, x.User.Email }
        })
        .ToListAsync();

    return Ok(requests);
}
[HttpGet("my")]
[Authorize(Roles = "User,Admin")]
public async Task<IActionResult> MyRequests(CancellationToken cancellationToken)
{
    if (!TryGetUserId(out var userId) ||
        !await _context.Users.AnyAsync(user => user.Id == userId, cancellationToken))
    {
        return Unauthorized(new { message = "A valid authenticated user is required." });
    }

    var data = await _context.ProductRequests
        .Where(x => x.UserId == userId)
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync(cancellationToken);

    return Ok(data);
}
//[Authorize(Roles = "Admin")]




[HttpPut("approve/{id}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> ApproveRequest(int id, CancellationToken cancellationToken)
{
    await using var transaction =
        await _context.Database.BeginTransactionAsync(cancellationToken);
    var request = await _context.ProductRequests
        .FromSqlInterpolated($"SELECT * FROM \"ProductRequests\" WHERE \"Id\" = {id} FOR UPDATE")
        .FirstOrDefaultAsync(cancellationToken);

    if (request == null)
    {
        return NotFound(new { message = "Request not found." });
    }

    if (request.Status != "Pending")
    {
        return Conflict(new { message = "Only pending requests can be approved." });
    }

    request.Status = "Approved";

    await _notificationEvents.CreateBroadcastAsync(
        $"A product request for {request.ProductName} has been accepted.",
        $"product-request:{request.Id}:approved:broadcast",
        NotificationEventService.BroadcastTopic,
        cancellationToken);

    await _context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);

    return Ok(new
    {
        message = "Request approved successfully."
    });
}

[HttpPut("reject/{id}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> RejectRequest(int id, CancellationToken cancellationToken)
{
    await using var transaction =
        await _context.Database.BeginTransactionAsync(cancellationToken);
    var request = await _context.ProductRequests
        .FromSqlInterpolated($"SELECT * FROM \"ProductRequests\" WHERE \"Id\" = {id} FOR UPDATE")
        .FirstOrDefaultAsync(cancellationToken);

    if (request == null)
    {
        return NotFound(new { message = "Request not found." });
    }

    if (request.Status != "Pending")
    {
        return Conflict(new { message = "Only pending requests can be rejected." });
    }

    request.Status = "Rejected";

    await _notificationEvents.CreateBroadcastAsync(
        $"A product request for {request.ProductName} was rejected.",
        $"product-request:{request.Id}:rejected:broadcast",
        NotificationEventService.BroadcastTopic,
        cancellationToken);

    await _context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);

    return Ok(new
    {
        message = "Request rejected successfully."
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
}}