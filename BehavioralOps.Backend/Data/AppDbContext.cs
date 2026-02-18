using BehavioralOps.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace BehavioralOps.Backend.Data;

public class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

  public DbSet<User> Users => Set<User>();
  public DbSet<Event> Events => Set<Event>();
  public DbSet<Cohort> Cohorts => Set<Cohort>();
  public DbSet<CohortLog> CohortLogs => Set<CohortLog>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Store enum as string for readability
    modelBuilder.Entity<CohortLog>()
        .Property(c => c.Action)
        .HasConversion<string>();

    // Seed block types as a reference (optional, for documentation)
    modelBuilder.Entity<User>().HasIndex(u => u.ExternalId).IsUnique();
  }
}
