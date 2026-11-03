using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;
using ShopAPI.DTOs;
using ShopAPI.Models;

namespace ShopAPI.Controllers
{
    [ApiController]
    [Route("api/festivals")]
    [Authorize(Roles = "Admin")]
    public class FestivalsController : ControllerBase
    {
        private static readonly TimeZoneInfo MaharashtraTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        private readonly AppDbContext _context;

        public FestivalsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var festivals = await _context.FestivalSchedules
                .OrderBy(festival => festival.FestivalDate)
                .ThenBy(festival => festival.Name)
                .ToListAsync(cancellationToken);

            return Ok(festivals);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var festival = await _context.FestivalSchedules
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return festival is null ? NotFound() : Ok(festival);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateFestivalScheduleDto request,
            CancellationToken cancellationToken)
        {
            var name = request.Name.Trim();
            if (name.Length < 2)
            {
                return BadRequest(new { message = "Festival name must contain at least 2 characters." });
            }

            var todayInMaharashtra = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MaharashtraTimeZone).Date);

            if (request.FestivalDate < todayInMaharashtra)
            {
                return BadRequest(new { message = "Festival date cannot be in the past." });
            }

            var alreadyExists = await _context.FestivalSchedules.AnyAsync(
                festival => festival.Name == name && festival.FestivalDate == request.FestivalDate,
                cancellationToken);

            if (alreadyExists)
            {
                return Conflict(new { message = "This festival is already scheduled for that date." });
            }

            var festival = new FestivalSchedule
            {
                Name = name,
                FestivalDate = request.FestivalDate,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.FestivalSchedules.Add(festival);
            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = festival.Id }, festival);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] CreateFestivalScheduleDto request,
            CancellationToken cancellationToken)
        {
            var festival = await _context.FestivalSchedules
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (festival is null)
            {
                return NotFound();
            }

            var name = request.Name.Trim();
            if (name.Length < 2)
            {
                return BadRequest(new { message = "Festival name must contain at least 2 characters." });
            }

            var todayInMaharashtra = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MaharashtraTimeZone).Date);

            if (request.FestivalDate < todayInMaharashtra)
            {
                return BadRequest(new { message = "Festival date cannot be in the past." });
            }

            var duplicate = await _context.FestivalSchedules.AnyAsync(
                item => item.Id != id &&
                        item.Name == name &&
                        item.FestivalDate == request.FestivalDate,
                cancellationToken);

            if (duplicate)
            {
                return Conflict(new { message = "This festival is already scheduled for that date." });
            }

            festival.Name = name;
            festival.FestivalDate = request.FestivalDate;
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(festival);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
        {
            var festival = await _context.FestivalSchedules
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (festival is null)
            {
                return NotFound();
            }

            festival.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}