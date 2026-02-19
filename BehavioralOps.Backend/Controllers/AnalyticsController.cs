using BehavioralOps.Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using BehavioralOps.Backend.Models;
using System.Text;
using System.Globalization;
using System.IO;

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
    var events = await _db.Events.ToListAsync();
    int audienceCount = 0;
    int activityCount = 0;

    var filterBlocks = request.Blocks.Where(IsFilterBlock).ToArray();
    // For audienceCount, we need to adjust operators to match the subset of blocks
    // This is getting complex, so for audienceCount we'll just check if they match ALL the filter blocks for now
    // Or we could try to filter the operator list too, but simpler is to just use a helper

    foreach (var user in users)
    {
      var userEvents = events.Where(e => e.UserId == user.Id).ToList();
      var props = JsonSerializer.Deserialize<Dictionary<string, object>>(user.Properties) ?? new();

      // audienceCount: Only match user property filters
      if (EvaluateFilterOnly(props, filterBlocks))
      {
        audienceCount++;

        // activityCount: Only if they match the full cohort definition AND were in the audience
        if (EvaluateUser(user, userEvents, request.Blocks, request.Operators))
        {
          activityCount++;
        }
      }
    }

    return Ok(new CountResponse(audienceCount, activityCount));
  }

  private bool IsFilterBlock(string blockId)
  {
    return blockId switch
    {
      "region_asia" or "region_eu" or "region_usa" or "high_value" or "churn_risk" => true,
      _ => false
    };
  }

  private bool EvaluateFilterOnly(Dictionary<string, object> props, string[] blocks)
  {
    if (blocks.Length == 0) return true; // Empty filter means everyone is the audience

    // Simplification: Audience is everyone matching ALL current filter blocks (AND logic)
    // regardless of the complex AND/OR operators used in the full builder
    foreach (var block in blocks)
    {
      if (!MatchesBlock(props, new List<Event>(), block)) return false;
    }
    return true;
  }

  private bool EvaluateUser(User user, List<Event> userEvents, string[] blocks, string[] operators)
  {
    if (blocks == null || blocks.Length == 0) return false;

    var props = JsonSerializer.Deserialize<Dictionary<string, object>>(user.Properties) ?? new();
    bool result = MatchesBlock(props, userEvents, blocks[0]);

    for (int i = 1; i < blocks.Length; i++)
    {
      string op = (operators != null && operators.Length >= i) ? operators[i - 1] : "AND";
      bool matchesNext = MatchesBlock(props, userEvents, blocks[i]);

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

  private bool MatchesBlock(Dictionary<string, object> props, List<Event> userEvents, string blockId)
  {
    return blockId switch
    {
      "region_asia" => props.TryGetValue("region", out var r) && r.ToString() == "Asia",
      "region_eu" => props.TryGetValue("region", out var r) && r.ToString() == "EU",
      "region_usa" => props.TryGetValue("region", out var r) && r.ToString() == "USA",
      "high_value" => props.TryGetValue("high_value", out var hv) && hv is bool b && b,
      "churn_risk" => props.TryGetValue("churn_risk", out var cr) && cr is bool b && b,
      "user_login" => userEvents.Any(e => e.Type == "user_login"),
      "purchase" => userEvents.Any(e => e.Type == "purchase"),
      "page_view" => userEvents.Any(e => e.Type == "page_view"),
      "user_signup" => userEvents.Any(e => e.Type == "user_signup"),
      _ => true
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

  [HttpGet("export")]
  public async Task<IActionResult> Export([FromQuery] string blocks, [FromQuery] string operators, [FromQuery] string fileType, [FromQuery] string columns)
  {
    var blockList = JsonSerializer.Deserialize<string[]>(blocks) ?? Array.Empty<string>();
    var operatorList = JsonSerializer.Deserialize<string[]>(operators) ?? Array.Empty<string>();
    var selectedColumns = JsonSerializer.Deserialize<string[]>(columns) ?? Array.Empty<string>();

    var users = await _db.Users.ToListAsync();
    var members = new List<Dictionary<string, object>>();
    var events = await _db.Events.ToListAsync();

    foreach (var user in users)
    {
      var userEvents = events.Where(e => e.UserId == user.Id).ToList();
      if (EvaluateUser(user, userEvents, blockList, operatorList))
      {
        var props = JsonSerializer.Deserialize<Dictionary<string, object>>(user.Properties) ?? new();
        var row = new Dictionary<string, object>();

        if (selectedColumns.Contains("user_id")) row["user_id"] = user.ExternalId;

        foreach (var col in selectedColumns)
        {
          if (col == "user_id") continue;
          if (props.TryGetValue(col, out var val))
          {
            row[col] = val?.ToString() ?? "";
          }
          else
          {
            row[col] = "";
          }
        }
        members.Add(row);
      }
    }

    if (fileType.ToUpper() == "JSON")
    {
      var json = JsonSerializer.Serialize(members, new JsonSerializerOptions { WriteIndented = true });
      return File(Encoding.UTF8.GetBytes(json), "application/json", "export.json");
    }
    else
    {
      using var ms = new MemoryStream();
      using (var writer = new StreamWriter(ms, Encoding.UTF8))
      using (var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture))
      {
        if (members.Any())
        {
          // Write header
          foreach (var key in members[0].Keys)
          {
            csv.WriteField(key);
          }
          csv.NextRecord();

          // Write data
          foreach (var row in members)
          {
            foreach (var val in row.Values)
            {
              csv.WriteField(val);
            }
            csv.NextRecord();
          }
        }
      }
      return File(ms.ToArray(), "text/csv", "export.csv");
    }
  }

  private async Task<int> CalculateCohortSize(string definition)
  {
    var blockIds = JsonSerializer.Deserialize<List<string>>(definition) ?? new();
    var users = await _db.Users.ToListAsync();
    var events = await _db.Events.ToListAsync();
    int count = 0;

    foreach (var user in users)
    {
      var userEvents = events.Where(e => e.UserId == user.Id).ToList();
      var props = JsonSerializer.Deserialize<Dictionary<string, object>>(user.Properties) ?? new();
      bool matchesAll = true;

      foreach (var blockId in blockIds)
      {
        bool matchesBlock = MatchesBlock(props, userEvents, blockId);

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
public record CountResponse(int audienceCount, int activityCount);
