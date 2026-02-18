using BehavioralOps.Backend.Data;
using BehavioralOps.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BehavioralOps.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;

    // The canonical block types supported by the drag-and-drop UI
    private static readonly string[] BlockTypes = new[]
    {
        "UserSignUp",
        "Purchase",
        "PageView",
        "Country",
        "DeviceType",
        "OnboardingFunnel"
    };

    public EventsController(AppDbContext db) => _db = db;

    // GET /api/events/types — returns available block types for the UI
    [HttpGet("types")]
    public IActionResult GetTypes() => Ok(BlockTypes);

    // GET /api/events?userId=&type=
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? userId, [FromQuery] string? type)
    {
        var query = _db.Events.Include(e => e.User).AsQueryable();

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(e => e.Type == type);

        var events = await query.OrderByDescending(e => e.Timestamp).ToListAsync();
        return Ok(events);
    }

    // POST /api/events — ingest a user behavioral event
    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] IngestEventRequest request)
    {
        if (!BlockTypes.Contains(request.Type, StringComparer.OrdinalIgnoreCase))
            return BadRequest($"Unknown event type '{request.Type}'. Valid types: {string.Join(", ", BlockTypes)}");

        // Resolve or create user by ExternalId
        var user = await _db.Users.FirstOrDefaultAsync(u => u.ExternalId == request.ExternalId);
        if (user == null)
        {
            user = new User { ExternalId = request.ExternalId, Properties = request.UserProperties ?? "{}" };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        var ev = new Event
        {
            UserId = user.Id,
            Type = request.Type,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            Metadata = request.Metadata ?? "{}"
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { userId = user.Id }, ev);
    }
}

public record IngestEventRequest(
    string ExternalId,
    string Type,
    DateTime? Timestamp,
    string? Metadata,
    string? UserProperties);
