
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BehavioralOps.Backend.Models;

public class Event
{
  public int Id { get; set; }

  public int UserId { get; set; }

  [ForeignKey("UserId")]
  public User? User { get; set; }

  [Required]
  public string Type { get; set; } = string.Empty; // e.g. "SignUp", "Purchase"

  public DateTime Timestamp { get; set; } = DateTime.UtcNow;

  public string Metadata { get; set; } = "{}"; // JSON context
}
