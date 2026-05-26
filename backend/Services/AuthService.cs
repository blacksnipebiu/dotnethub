
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DotNetHub.Server.Data;
using DotNetHub.Server.Models;

namespace DotNetHub.Server.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    
    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }
    
    public async Task<AuthResponse> Register(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            return new AuthResponse { Success = false, Message = "Username already exists" };
        
        var user = new User
        {
            Username = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = "user"
        };
        
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        
        var token = GenerateToken(user);
        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            Token = token,
            User = MapUser(user)
        };
    }
    
    public async Task<AuthResponse> Login(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return new AuthResponse { Success = false, Message = "Invalid credentials" };
        
        if (!user.IsActive)
            return new AuthResponse { Success = false, Message = "Account is disabled" };
        
        var token = GenerateToken(user);
        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            Token = token,
            User = MapUser(user)
        };
    }
    
    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Key"] ?? "DotNetHub_Super_Secret_Key_Min_32_Chars!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };
        
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "DotNetHub",
            audience: _config["Jwt:Audience"] ?? "DotNetHub",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    private static UserDto MapUser(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Role = u.Role,
        CreatedAt = u.CreatedAt,
        IsActive = u.IsActive
    };
}
