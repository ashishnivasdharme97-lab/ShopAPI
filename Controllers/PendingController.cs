
using Microsoft.AspNetCore.Mvc;
using ShopAPI.Data;
using ShopAPI.Models;
using ShopAPI.DTOs;


[ApiController]
[Route("api/[controller]")]
public class PendingController : ControllerBase
{
    private readonly AppDbContext _context;

    public PendingController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> AddPending(CreatePendingDto dto)
    {
        var pending = new Pending
        {
            CustomerName = dto.CustomerName,
            Amount = dto.Amount,
            Reason = dto.Reason,
            CreatedAt = DateTime.Now,
            Status = "Pending"
        };

        _context.Pendings.Add(pending);

        await _context.SaveChangesAsync();

        return Ok();
    }
}