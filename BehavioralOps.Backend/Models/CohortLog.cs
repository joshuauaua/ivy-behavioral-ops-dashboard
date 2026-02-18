using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BehavioralOps.Backend.Models;

public enum CohortAction
{
  Created,
  Updated,
  Exported,
  Scheduled
}

public class CohortLog
{
  public int Id { get; set; }

  public int CohortId { get; set; }

  [ForeignKey("CohortId")]
  public Cohort? Cohort { get; set; }

  [Required]
  public CohortAction Action { get; set; }

  public DateTime Timestamp { get; set; } = DateTime.UtcNow;

  // The dashboard admin user who performed the action
  [Required]
  public string AdminUser { get; set; } = string.Empty;
}
