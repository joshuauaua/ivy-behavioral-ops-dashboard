using BehavioralOps.Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BehavioralOps.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
  private readonly AppDbContext _db;

  public AnalyticsController(AppDbContext db) => _db = db;

  [HttpPost("count")]
  public async Task<IActionResult> GetCohortCount([FromBody] List<string> blockIds)
  {
    var users = await _db.Users.ToListAsync();
    int count = 0;

    foreach (var user in users)
    {
      var props = JsonSerializer.Deserialize<Dictionary<string, object>>(user.Properties) ?? new();
      bool matchesAll = true;

      foreach (var blockId in blockIds)
      {
        bool matchesBlock = blockId switch
        {
          "region_asia" => props.TryGetValue("region", out var r) && r.ToString() == "Asia",
          "region_eu" => props.TryGetValue("region", out var r) && r.ToString() == "EU",
          "region_usa" => props.TryGetValue("region", out var r) && r.ToString() == "USA",
          "high_value" => props.TryGetValue("high_value", out var hv) && hv is bool b && b,
          "churn_risk" => props.TryGetValue("churn_risk", out var cr) && cr is bool b && b,
          // Events could be handled by checking an Events table, but for now we'll simulate or match props
          "user_login" => true, // Simulate event match
          "purchase" => true,
          "page_view" => true,
          "user_signup" => true,
          _ => true
        };

        if (!matchesBlock)
        {
          matchesAll = false;
          break;
        }
      }

      if (matchesAll) count++;
    }

    return Ok(new { count });
  }
}
