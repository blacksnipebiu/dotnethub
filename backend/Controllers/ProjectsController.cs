
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DotNetHub.Server.Models;
using DotNetHub.Server.Services;

namespace DotNetHub.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly ProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;
    
    public ProjectsController(ProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll([FromQuery] bool? publicOnly)
    {
        // If authenticated, show user's own + public. Otherwise only public.
        var userId = GetUserId();
        int? filterUserId = userId;
        if (userId == null && publicOnly != false)
            publicOnly = true;
        
        return Ok(await _projectService.GetAll(userId, publicOnly));
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDto>> GetById(int id)
    {
        var project = await _projectService.GetById(id);
        if (project == null) return NotFound();
        return Ok(project);
    }
    
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] ProjectCreateRequest req)
    {
        try
        {
            var result = await _projectService.Create(req, GetUserId()!.Value);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<ProjectDto>> Update(int id, [FromBody] ProjectUpdateRequest req)
    {
        try
        {
            var result = await _projectService.Update(id, req, GetUserId()!.Value, GetUserRole()!);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var result = await _projectService.Delete(id, GetUserId()!.Value, GetUserRole()!);
            if (!result) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
    
    [Authorize]
    [HttpPost("{id}/upload")]
    public async Task<ActionResult> Upload(int id, [FromForm] IFormFileCollection files)
    {
        try
        {
            var result = await _projectService.UploadFiles(id, files, GetUserId()!.Value, GetUserRole()!);
            if (!result) return NotFound();
            return Ok(new { message = "Files uploaded" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
    
    [Authorize]
    [HttpPost("{id}/build")]
    public async Task<ActionResult> Build(int id)
    {
        try
        {
            var result = await _projectService.Build(id, GetUserId()!.Value, GetUserRole()!);
            if (!result) return BadRequest(new { message = "Build failed" });
            return Ok(new { message = "Build successful" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
    
    [Authorize]
    [HttpPost("{id}/deploy")]
    public async Task<ActionResult> Deploy(int id)
    {
        try
        {
            var result = await _projectService.Deploy(id, GetUserId()!.Value, GetUserRole()!);
            if (!result) return BadRequest(new { message = "Deploy failed" });
            return Ok(new { message = "Deployed successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
    
    [Authorize]
    [HttpPost("{id}/stop")]
    public async Task<ActionResult> Stop(int id)
    {
        try
        {
            var project = await _projectService.GetById(id);
            if (project == null) return NotFound();
            
            // Verify ownership or admin
            var userId = GetUserId()!.Value;
            var role = GetUserRole()!;
            if (project.OwnerName != User.FindFirstValue(ClaimTypes.Name) && role != "admin")
                return Forbid();
            
            await _projectService.Stop(id);
            return Ok(new { message = "Stopped" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
    
    private int? GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return idClaim != null ? int.Parse(idClaim) : null;
    }
    
    private string? GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }
}
