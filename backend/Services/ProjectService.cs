
using System.Diagnostics;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using DotNetHub.Server.Data;
using DotNetHub.Server.Models;

namespace DotNetHub.Server.Services;

public class ProjectService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<ProjectService> _logger;
    private static readonly Dictionary<int, Process> RunningProcesses = new();
    
    public ProjectService(AppDbContext db, IConfiguration config, ILogger<ProjectService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }
    
    public async Task<List<ProjectDto>> GetAll(int? userId = null, bool? onlyPublic = null)
    {
        var query = _db.Projects.Include(p => p.User).AsQueryable();
        
        if (userId.HasValue)
            query = query.Where(p => p.UserId == userId.Value);
        if (onlyPublic == true)
            query = query.Where(p => p.IsPublic);
            
        return await query.OrderByDescending(p => p.UpdatedAt)
            .Select(p => Map(p))
            .ToListAsync();
    }
    
    public async Task<ProjectDto?> GetById(int id)
    {
        return await _db.Projects.Include(p => p.User)
            .Where(p => p.Id == id)
            .Select(p => Map(p))
            .FirstOrDefaultAsync();
    }
    
    public async Task<ProjectDto> Create(ProjectCreateRequest req, int userId)
    {
        // Validate port
        if (await _db.Projects.AnyAsync(p => p.Port == req.Port && p.Status == "running"))
            throw new InvalidOperationException($"Port {req.Port} is already in use");
        
        var project = new Project
        {
            Name = req.Name,
            Description = req.Description,
            Port = req.Port,
            DotNetVersion = req.DotNetVersion,
            IsPublic = req.IsPublic,
            GitRepo = req.GitRepo,
            UserId = userId,
            Status = "stopped"
        };
        
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        
        // Create storage directory
        var storagePath = Path.Combine(_config["Storage:ProjectsPath"] ?? "/opt/data/dotnethub-projects", project.Id.ToString());
        Directory.CreateDirectory(storagePath);
        project.StoragePath = storagePath;
        await _db.SaveChangesAsync();
        
        return await GetById(project.Id) ?? throw new Exception("Failed to create project");
    }
    
    public async Task<ProjectDto?> Update(int id, ProjectUpdateRequest req, int userId, string userRole)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null) return null;
        if (project.UserId != userId && userRole != "admin")
            throw new UnauthorizedAccessException("Not authorized");
        
        if (req.Name != null) project.Name = req.Name;
        if (req.Description != null) project.Description = req.Description;
        if (req.GitRepo != null) project.GitRepo = req.GitRepo;
        if (req.IsPublic.HasValue) project.IsPublic = req.IsPublic.Value;
        if (req.Port.HasValue && req.Port.Value != project.Port)
        {
            if (await _db.Projects.AnyAsync(p => p.Port == req.Port.Value && p.Id != id && p.Status == "running"))
                throw new InvalidOperationException($"Port {req.Port.Value} is already in use");
            project.Port = req.Port.Value;
        }
        
        project.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await GetById(id);
    }
    
    public async Task<bool> Delete(int id, int userId, string userRole)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null) return false;
        if (project.UserId != userId && userRole != "admin")
            throw new UnauthorizedAccessException("Not authorized");
        
        // Stop if running
        if (project.Status == "running")
            await Stop(id);
        
        // Delete files
        if (Directory.Exists(project.StoragePath))
            Directory.Delete(project.StoragePath, true);
        
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> UploadFiles(int id, IFormFileCollection files, int userId, string userRole)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null) return false;
        if (project.UserId != userId && userRole != "admin")
            throw new UnauthorizedAccessException("Not authorized");
        
        var extractPath = project.StoragePath;
        // Clear existing files
        if (Directory.Exists(extractPath))
        {
            foreach (var file in Directory.GetFiles(extractPath)) File.Delete(file);
            foreach (var dir in Directory.GetDirectories(extractPath)) Directory.Delete(dir, true);
        }
        
        foreach (var file in files)
        {
            if (file.FileName.EndsWith(".zip"))
            {
                using var stream = file.OpenReadStream();
                using var archive = new ZipArchive(stream);
                archive.ExtractToDirectory(extractPath, true);
            }
            else
            {
                var filePath = Path.Combine(extractPath, file.FileName);
                using var stream = File.Create(filePath);
                await file.CopyToAsync(stream);
            }
        }
        
        project.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> Build(int id, int userId, string userRole)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null) return false;
        if (project.UserId != userId && userRole != "admin")
            throw new UnauthorizedAccessException("Not authorized");
        
        project.Status = "building";
        await _db.SaveChangesAsync();
        
        try
        {
            var dotnet = "/usr/share/dotnet/dotnet";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = dotnet,
                    Arguments = "build -c Release",
                    WorkingDirectory = project.StoragePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            await process.WaitForExitAsync(cts.Token);
            
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                _logger.LogError("Build failed for project {Id}: {Error}", id, error);
                project.Status = "error";
                await _db.SaveChangesAsync();
                return false;
            }
            
            project.Status = "stopped";
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Build exception for project {Id}", id);
            project.Status = "error";
            await _db.SaveChangesAsync();
            return false;
        }
    }
    
    public async Task<bool> Deploy(int id, int userId, string userRole)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null) return false;
        if (project.UserId != userId && userRole != "admin")
            throw new UnauthorizedAccessException("Not authorized");
        
        if (project.Status == "running")
            await Stop(id);
        
        project.Status = "running";
        
        try
        {
            var dotnet = "/usr/share/dotnet/dotnet";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = dotnet,
                    Arguments = $"run -c Release --urls http://0.0.0.0:{project.Port}",
                    WorkingDirectory = project.StoragePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            
            process.Start();
            project.ProcessId = process.Id;
            lock (RunningProcesses)
            {
                RunningProcesses[id] = process;
            }
            
            await _db.SaveChangesAsync();
            
            // Wait a moment and check
            await Task.Delay(2000);
            if (process.HasExited)
            {
                project.Status = "error";
                project.ProcessId = null;
                await _db.SaveChangesAsync();
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deploy exception for project {Id}", id);
            project.Status = "error";
            await _db.SaveChangesAsync();
            return false;
        }
    }
    
    public async Task<bool> Stop(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null) return false;
        
        lock (RunningProcesses)
        {
            if (RunningProcesses.TryGetValue(id, out var process))
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
                process.Dispose();
                RunningProcesses.Remove(id);
            }
        }
        
        project.Status = "stopped";
        project.ProcessId = null;
        await _db.SaveChangesAsync();
        return true;
    }
    
    private static ProjectDto Map(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Port = p.Port,
        Status = p.Status,
        DotNetVersion = p.DotNetVersion,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        IsPublic = p.IsPublic,
        GitRepo = p.GitRepo,
        OwnerName = p.User?.Username
    };
}
