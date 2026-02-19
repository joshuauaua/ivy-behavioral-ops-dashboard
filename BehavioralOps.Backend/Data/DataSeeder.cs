using BehavioralOps.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace BehavioralOps.Backend.Data;

public static class DataSeeder
{
  public static void Seed(AppDbContext db)
  {
    db.Database.Migrate();

    if (db.Users.Count() < 200)
    {
      db.Users.RemoveRange(db.Users);
      db.SaveChanges();

      var random = new Random();
      var users = new List<User>();
      var countries = new[] { ("USA", "USA"), ("UK", "EU"), ("Germany", "EU"), ("China", "Asia"), ("India", "Asia"), ("Japan", "Asia"), ("France", "EU"), ("Canada", "USA") };

      for (int i = 1; i <= 200; i++)
      {
        var countryInfo = countries[random.Next(countries.Length)];
        var signupDate = DateTime.UtcNow.AddDays(-random.Next(1, 365));
        var lastSeen = signupDate.AddDays(random.Next(0, (DateTime.UtcNow - signupDate).Days));

        var properties = new
        {
          email = $"user_{i:D3}@example.com",
          signup_date = signupDate.ToString("yyyy-MM-dd"),
          last_seen = lastSeen.ToString("yyyy-MM-dd"),
          country = countryInfo.Item1,
          region = countryInfo.Item2,
          high_value = random.Next(10) < 3, // 30% high value
          churn_risk = random.Next(10) < 2  // 20% churn risk
        };

        users.Add(new User
        {
          ExternalId = $"user_{i:D3}",
          Properties = System.Text.Json.JsonSerializer.Serialize(properties)
        });
      }

      db.Users.AddRange(users);
      db.SaveChanges();

      var eventTypes = new[] { "user_login", "purchase", "page_view", "user_signup" };
      var events = new List<Event>();

      foreach (var user in users)
      {
        // Always add a signup event
        events.Add(new Event
        {
          UserId = user.Id,
          Type = "user_signup",
          Timestamp = DateTime.UtcNow.AddDays(-random.Next(30, 60))
        });

        // Add 5-15 random events
        int eventCount = random.Next(5, 16);
        for (int j = 0; j < eventCount; j++)
        {
          events.Add(new Event
          {
            UserId = user.Id,
            Type = eventTypes[random.Next(eventTypes.Length)],
            Timestamp = DateTime.UtcNow.AddDays(-random.Next(0, 30)),
            Metadata = "{ \"source\": \"web\" }"
          });
        }
      }

      db.Events.AddRange(events);
      db.SaveChanges();
    }

    if (!db.Cohorts.Any())
    {
      db.Cohorts.AddRange(new List<Cohort>
          {
              new() { Name = "Active US Users", Definition = "[\"region_usa\"]", CreatedAt = DateTime.UtcNow.AddDays(-5) },
              new() { Name = "Recent Signups (EU)", Definition = "[\"region_eu\"]", CreatedAt = DateTime.UtcNow.AddDays(-2) },
              new() { Name = "High Value Prospects", Definition = "[\"high_value\"]", CreatedAt = DateTime.UtcNow.AddDays(-1) }
          });
      db.SaveChanges();
    }
  }
}
