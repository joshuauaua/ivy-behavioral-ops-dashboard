using BehavioralOps.Backend.Data;
using BehavioralOps.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BehavioralOps.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CohortsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CohortsController(AppDbContext db) => _db = db;

    // GET /api/cohorts
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cohorts = await _db.Cohorts
            .Include(c => c.Logs)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return Ok(cohorts);
    }

    // GET /api/cohorts/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cohort = await _db.Cohorts
            .Include(c => c.Logs)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cohort == null) return NotFound();
        return Ok(cohort);
    }

    // POST /api/cohorts
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCohortRequest request)
    {
        var cohort = new Cohort
        {
            Name = request.Name,
            Definition = request.Definition ?? "[]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Cohorts.Add(cohort);
        await _db.SaveChangesAsync();

        _db.CohortLogs.Add(new CohortLog
        {
            CohortId = cohort.Id,
            Action = CohortAction.Created,
            Timestamp = DateTime.UtcNow,
            AdminUser = request.AdminUser ?? "system"
        });
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = cohort.Id }, cohort);
    }

    // PUT /api/cohorts/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCohortRequest request)
    {
        var cohort = await _db.Cohorts.FindAsync(id);
        if (cohort == null) return NotFound();

        cohort.Name = request.Name ?? cohort.Name;
        cohort.Definition = request.Definition ?? cohort.Definition;
        cohort.UpdatedAt = DateTime.UtcNow;

        _db.CohortLogs.Add(new CohortLog
        {
            CohortId = cohort.Id,
            Action = CohortAction.Updated,
            Timestamp = DateTime.UtcNow,
            AdminUser = request.AdminUser ?? "system"
        });

        await _db.SaveChangesAsync();
        return Ok(cohort);
    }

    // POST /api/cohorts/{id}/action
    [HttpPost("{id}/action")]
    public async Task<IActionResult> LogAction(int id, [FromBody] CohortActionRequest request)
    {
        var cohort = await _db.Cohorts.FindAsync(id);
        if (cohort == null) return NotFound();

        if (!Enum.TryParse<CohortAction>(request.Action, true, out var action))
            return BadRequest($"Invalid action. Valid values: {string.Join(", ", Enum.GetNames<CohortAction>())}");

        var log = new CohortLog
        {
            CohortId = cohort.Id,
            Action = action,
            Timestamp = DateTime.UtcNow,
            AdminUser = request.AdminUser ?? "system"
        };

        _db.CohortLogs.Add(log);
        await _db.SaveChangesAsync();

        return Ok(log);
    }

    // DELETE /api/cohorts/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cohort = await _db.Cohorts.FindAsync(id);
        if (cohort == null) return NotFound();

        _db.Cohorts.Remove(cohort);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateCohortRequest(string Name, string? Definition, string? AdminUser);
public record UpdateCohortRequest(string? Name, string? Definition, string? AdminUser);
public record CohortActionRequest(string Action, string? AdminUser);
