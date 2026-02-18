using BehavioralOps.Backend.Data;
using BehavioralOps.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BehavioralOps.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    // GET /api/users
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users.ToListAsync();
        return Ok(users);
    }

    // GET /api/users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    // POST /api/users — bulk import users
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] List<ImportUserRequest> requests)
    {
        var created = new List<User>();
        foreach (var req in requests)
        {
            var existing = await _db.Users.FirstOrDefaultAsync(u => u.ExternalId == req.ExternalId);
            if (existing != null)
            {
                existing.Properties = req.Properties ?? existing.Properties;
                continue;
            }

            var user = new User
            {
                ExternalId = req.ExternalId,
                Properties = req.Properties ?? "{}"
            };
            _db.Users.Add(user);
            created.Add(user);
        }

        await _db.SaveChangesAsync();
        return Ok(new { imported = created.Count, total = requests.Count });
    }

    // DELETE /api/users/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record ImportUserRequest(string ExternalId, string? Properties);
