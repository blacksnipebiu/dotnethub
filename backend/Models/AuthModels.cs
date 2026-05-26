
using System.ComponentModel.DataAnnotations;

namespace DotNetHub.Server.Models;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = "";
    
    [Required]
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    [Required, MinLength(3), MaxLength(50)]
    public string Username { get; set; } = "";
    
    [Required, MinLength(6)]
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Token { get; set; }
    public UserDto? User { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
