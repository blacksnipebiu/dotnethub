
using System.ComponentModel.DataAnnotations;

namespace DotNetHub.Server.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required, MaxLength(50)]
    public string Username { get; set; } = "";
    
    [Required]
    public string PasswordHash { get; set; } = "";
    
    [Required, MaxLength(20)]
    public string Role { get; set; } = "user"; // user or admin
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
}
