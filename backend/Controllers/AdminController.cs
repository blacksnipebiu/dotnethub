
using System.Security.Claims;
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

        // Cannot disable self
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (user.Id == currentUserId)
            return BadRequest(new { message = "不能对自己执行此操作" });

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"用户{(user.IsActive ? "已激活" : "已禁用")}" });
    }
    
    [HttpPut("users/{id}/role")]
    public async Task<ActionResult> ChangeRole(int id, [FromBody] ChangeRoleRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (req.Role is not ("user" or "admin"))
            return BadRequest(new { message = "无效的角色" });

        // Cannot change own role
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (user.Id == currentUserId)
            return BadRequest(new { message = "不能修改自己的角色" });

        user.Role = req.Role;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"角色已变更为 {req.Role}" });
    }

    [HttpPut("change-password")]
    [Authorize]  // Override class-level admin requirement — any logged-in user
    public async Task<ActionResult> ChangeOwnPassword([FromBody] ChangePasswordRequest req)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "当前密码不正确" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = "密码修改成功" });
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

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
