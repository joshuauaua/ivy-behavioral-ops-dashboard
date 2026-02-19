using BehavioralOps.Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using BehavioralOps.Backend.Models;

namespace BehavioralOps.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
  private readonly AppDbContext _db;

  public AnalyticsController(AppDbContext db) => _db = db;

  [HttpPost("count")]
  public async Task<IActionResult> GetCohortCount([FromBody] CountRequest request)
  {
    var users = await _db.Users.ToListAsync();
    int count = 0;

    foreach (var user in users)
    {
      if (EvaluateUser(user, request.Blocks, request.Operators))
      {
        count++;
      }
    }

    return Ok(new { count });
  }

  private bool EvaluateUser(User user, string[] blocks, string[] operators)
  {
    if (blocks == null || blocks.Length == 0) return false;

    var props = JsonSerializer.Deserialize<Dictionary<string, object>>(user.Properties) ?? new();
    bool result = MatchesBlock(props, blocks[0]);

    for (int i = 1; i < blocks.Length; i++)
    {
      string op = (operators != null && operators.Length >= i) ? operators[i - 1] : "AND";
      bool matchesNext = MatchesBlock(props, blocks[i]);

      if (op == "OR")
      {
        result = result || matchesNext;
      }
      else
      {
        result = result && matchesNext;
      }
    }

    return result;
  }

  private bool MatchesBlock(Dictionary<string, object> props, string blockId)
  {
    return blockId switch
    {
      "region_asia" => props.TryGetValue("region", out var r) && r.ToString() == "Asia",
      "region_eu" => props.TryGetValue("region", out var r) && r.ToString() == "EU",
      "region_usa" => props.TryGetValue("region", out var r) && r.ToString() == "USA",
      "high_value" => props.TryGetValue("high_value", out var hv) && hv is bool b && b,
      "churn_risk" => props.TryGetValue("churn_risk", out var cr) && cr is bool b && b,
      _ => true // Events are simulated as true for now
    };
  }

  [HttpGet("stats")]
  public async Task<IActionResult> GetStats()
  {
    var totalUsers = await _db.Users.CountAsync();
    var cohorts = await _db.Cohorts.ToListAsync();
    var totalCohorts = cohorts.Count;

    // Calculate average size based on real matching
    double totalSize = 0;
    foreach (var cohort in cohorts)
    {
      totalSize += await CalculateCohortSize(cohort.Definition);
    }
    double avgSize = totalCohorts > 0 ? totalSize / totalCohorts : 0;

    return Ok(new { totalUsers, totalCohorts, avgSize });
  }

  [HttpGet("distribution")]
  public async Task<IActionResult> GetDistribution()
  {
    var cohorts = await _db.Cohorts.ToListAsync();
    var sizes = new List<int>();
    foreach (var cohort in cohorts)
    {
      sizes.Add(await CalculateCohortSize(cohort.Definition));
    }

    var distribution = new[]
    {
      new { range = "0-40", count = sizes.Count(s => s <= 40) },
      new { range = "41-80", count = sizes.Count(s => s > 40 && s <= 80) },
      new { range = "81-120", count = sizes.Count(s => s > 80 && s <= 120) },
      new { range = "121-160", count = sizes.Count(s => s > 120 && s <= 160) },
      new { range = "161-200", count = sizes.Count(s => s > 160) }
    };

    return Ok(distribution);
  }

  [HttpGet("trend")]
  public async Task<IActionResult> GetTrend()
  {
    var cohorts = await _db.Cohorts.ToListAsync();
    var trend = cohorts
        .GroupBy(c => c.CreatedAt.ToString("MMM"))
        .Select(g => new { Month = g.Key, Cohorts = g.Count() })
        .ToList();

    // Ensure we have some months even if data is thin
    return Ok(trend);
  }

  [HttpGet("activity")]
  public async Task<IActionResult> GetActivity()
  {
    var logs = await _db.CohortLogs
        .Include(l => l.Cohort)
        .OrderByDescending(l => l.Timestamp)
        .Take(10)
        .Select(l => new
        {
          Cohort = l.Cohort != null ? l.Cohort.Name : "Unknown",
          Action = l.Action.ToString(),
          Timestamp = FormatTimestamp(l.Timestamp),
          User = l.AdminUser
        })
        .ToListAsync();

    return Ok(logs);
  }

  private async Task<int> CalculateCohortSize(string definition)
  {
    var blockIds = JsonSerializer.Deserialize<List<string>>(definition) ?? new();
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

    return count;
  }

  private static string FormatTimestamp(DateTime dt)
  {
    var span = DateTime.UtcNow - dt;
    if (span.TotalMinutes < 1) return "Just now";
    if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} mins ago";
    if (span.TotalHours < 24) return $"{(int)span.TotalHours} hours ago";
    return dt.ToString("MMM dd, yyyy");
  }
}

public record CountRequest(string[] Blocks, string[] Operators);
