using BehavioralOps.Backend.Data;
using BehavioralOps.Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=behavioral_ops.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  db.Database.Migrate();

  if (db.Users.Count() < 200)
  {
    db.Users.RemoveRange(db.Users);
    db.SaveChanges();

    var random = new Random();
    var users = new List<User>();
    var countries = new[] { ("USA", "USA"), ("China", "Asia"), ("India", "Asia"), ("UK", "EU"), ("Germany", "EU") };

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
  }
}

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
