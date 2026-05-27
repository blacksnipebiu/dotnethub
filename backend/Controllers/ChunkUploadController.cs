using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DotNetHub.Server.Models;
using DotNetHub.Server.Services;

namespace DotNetHub.Server.Controllers;

[ApiController]
[Route("api/projects")]
public class ChunkUploadController : ControllerBase
{
    private readonly ProjectService _projectService;
    private readonly ChunkUploadService _chunkService;
    
    public ChunkUploadController(ProjectService projectService, ChunkUploadService chunkService)
    {
        _projectService = projectService;
        _chunkService = chunkService;
    }
    
    [Authorize]
    [HttpPost("{id}/upload-chunk")]
    public async Task<ActionResult<ChunkStatus>> UploadChunk(
        int id,
        [FromForm] IFormFile file,
        [FromForm] int chunkIndex,
        [FromForm] int totalChunks,
        [FromForm] string fileName)
    {
        if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "只允许上传 .zip 文件" });

        var project = await _projectService.GetById(id);
        if (project == null) return NotFound();
        
        using var stream = file.OpenReadStream();
        var status = await _chunkService.SaveChunk(project.StoragePath, fileName, chunkIndex, totalChunks, stream);
        return Ok(status);
    }
    
    [Authorize]
    [HttpGet("{id}/upload-status")]
    public async Task<ActionResult<ChunkStatus>> GetUploadStatus(
        int id,
        [FromQuery] string fileName,
        [FromQuery] int totalChunks)
    {
        var project = await _projectService.GetById(id);
        if (project == null) return NotFound();
        return Ok(_chunkService.GetStatus(project.StoragePath, fileName, totalChunks));
    }
    
    [Authorize]
    [HttpPost("{id}/upload-chunk/clear")]
    public async Task<ActionResult> ClearChunks(int id)
    {
        var project = await _projectService.GetById(id);
        if (project == null) return NotFound();
        _chunkService.CleanChunks(project.StoragePath);
        return Ok(new { message = "已清空" });
    }
    
    [Authorize]
    [HttpPost("{id}/upload-chunk/combine")]
    public async Task<ActionResult> CombineChunks(
        int id,
        [FromBody] CombineRequest req)
    {
        var project = await _projectService.GetById(id);
        if (project == null) return NotFound();
        
        try
        {
            await _chunkService.CombineAndExtract(
                project.StoragePath, req.FileName, project.Name, req.TotalChunks);
            return Ok(new { message = "解压完成" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"合并解压失败：{ex.Message}" });
        }
    }
}

public class CombineRequest
{
    public string FileName { get; set; } = "";
    public int TotalChunks { get; set; }
}
