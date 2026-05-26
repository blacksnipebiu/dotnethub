
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotNetHub.Server.Data;
using DotNetHub.Server.Models;

namespace DotNetHub.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    
    public AdminController(AppDbContext db) => _db = db;
    
    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        return await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserDto
            {
                Id = u.Id, Username = u.Username, Role = u.Role,
                CreatedAt = u.CreatedAt, IsActive = u.IsActive
            })
            .ToListAsync();
    }
    
    [HttpPut("users/{id}/toggle-active")]
    public async Task<ActionResult> ToggleUserActive(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"User {(user.IsActive ? "activated" : "deactivated")}" });
    }
    
    [HttpPut("users/{id}/role")]
    public async Task<ActionResult> ChangeRole(int id, [FromBody] ChangeRoleRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (req.Role is not ("user" or "admin"))
            return BadRequest(new { message = "Invalid role" });
        user.Role = req.Role;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Role changed to {req.Role}" });
    }
    
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        var totalUsers = await _db.Users.CountAsync();
        var totalProjects = await _db.Projects.CountAsync();
        var runningProjects = await _db.Projects.CountAsync(p => p.Status == "running");
        return Ok(new { totalUsers, totalProjects, runningProjects });
    }
}

public class ChangeRoleRequest
{
    public string Role { get; set; } = "user";
}
