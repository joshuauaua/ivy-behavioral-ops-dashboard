
using System.ComponentModel.DataAnnotations;

namespace BehavioralOps.Backend.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    public string ExternalId { get; set; } = string.Empty;

    // Storing dynamic properties as a JSON string for SQLite simplicity
    public string Properties { get; set; } = "{}"; 
}
