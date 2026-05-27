
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotNetHub.Server.Data;
using DotNetHub.Server.Models;

namespace DotNetHub.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    
    public SettingsController(AppDbContext db) => _db = db;
    
    [HttpGet]
    public async Task<ActionResult<List<SystemSettingDto>>> GetAll()
    {
        return await _db.SystemSettings
            .Select(s => new SystemSettingDto
            {
                Key = s.Key,
                Value = s.Value,
                Description = s.Description
            })
            .ToListAsync();
    }
    
    [Authorize(Roles = "admin")]
    [HttpPut("{key}")]
    public async Task<ActionResult> Update(string key, [FromBody] SystemSettingUpdateRequest req)
    {
        var setting = await _db.SystemSettings.FindAsync(key);
        if (setting == null) return NotFound();
        setting.Value = req.Value;
        await _db.SaveChangesAsync();
        return Ok(new { message = "设置已保存" });
    }
}
