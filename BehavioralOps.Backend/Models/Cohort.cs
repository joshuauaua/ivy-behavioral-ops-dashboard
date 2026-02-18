using System.ComponentModel.DataAnnotations;

namespace BehavioralOps.Backend.Models;

public class Cohort
{
  public int Id { get; set; }

  [Required]
  public string Name { get; set; } = string.Empty;

  // JSON blob representing the drag-and-drop block filter definition
  public string Definition { get; set; } = "[]";

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public ICollection<CohortLog> Logs { get; set; } = new List<CohortLog>();
}
