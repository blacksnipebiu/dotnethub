
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using DotNetHub.Server.Data;
using DotNetHub.Server.Models;

namespace DotNetHub.Server.Services;

public class ProjectService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<ProjectService> _logger;
    private readonly string _storageRoot;
    private static readonly Dictionary<int, Process> RunningProcesses = new();
    
    public ProjectService(AppDbContext db, IConfiguration config, ILogger<ProjectService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
        _storageRoot = config["Storage:ProjectsPath"] ?? "projects";
        if (!Path.IsPathRooted(_storageRoot))
            _storageRoot = Path.GetFullPath(_storageRoot);
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
        var storagePath = Path.Combine(_storageRoot, project.Id.ToString());
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
        if (req.StartupArgs != null) project.StartupArgs = req.StartupArgs;
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

        // Check if this is a published package (has .dll/.exe, no .csproj)
        var isPublished = IsPublishedPackage(project.StoragePath);
        
        if (isPublished)
        {
            _logger.LogInformation("Project {Id} is a published package, skipping build", id);
            project.Status = "stopped";
            await _db.SaveChangesAsync();
            return true;
        }
        
        project.Status = "building";
        await _db.SaveChangesAsync();
        
        try
        {
            var dotnet = GetDotNetPath();
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
                var err = (await process.StandardError.ReadToEndAsync()).Trim();
                if (string.IsNullOrEmpty(err)) err = await process.StandardOutput.ReadToEndAsync();
                project.Status = "error";
                await _db.SaveChangesAsync();
                throw new InvalidOperationException($"构建失败（退出码 {process.ExitCode}）：{err}");
            }
            
            project.Status = "stopped";
            await _db.SaveChangesAsync();
            return true;
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Build exception for project {Id}", id);
            project.Status = "error";
            await _db.SaveChangesAsync();
            throw new InvalidOperationException($"构建异常：{ex.Message}", ex);
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
            var (command, args) = GetDeployCommand(project);
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
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
            
            await Task.Delay(2000);
            if (process.HasExited)
            {
                var exitErr = "";
                try { exitErr = process.StandardError.ReadToEnd().Trim(); } catch { }
                if (string.IsNullOrEmpty(exitErr)) try { exitErr = process.StandardOutput.ReadToEnd().Trim(); } catch { }
                project.Status = "error";
                project.ProcessId = null;
                await _db.SaveChangesAsync();
                throw new InvalidOperationException($"部署失败（退出码 {process.ExitCode}）：{exitErr}");
            }
            
            return true;
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deploy exception for project {Id}", id);
            project.Status = "error";
            await _db.SaveChangesAsync();
            throw new InvalidOperationException($"部署异常：{ex.Message}", ex);
        }
    }

    private static string GetDotNetPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "dotnet";
        return "/usr/share/dotnet/dotnet";
    }

    private bool IsPublishedPackage(string path)
    {
        if (!Directory.Exists(path)) return false;
        // Published package: has .dll or .exe files, no .csproj or .sln
        var hasDll = Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly).Any();
        var hasExe = Directory.GetFiles(path, "*.exe", SearchOption.TopDirectoryOnly).Any();
        var hasProject = Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Any()
            || Directory.GetFiles(path, "*.sln", SearchOption.TopDirectoryOnly).Any();
        return (hasDll || hasExe) && !hasProject;
    }

    private (string command, string args) GetDeployCommand(Project project)
    {
        var dotnet = GetDotNetPath();
        var port = project.Port;
        
        // If published package, find the main dll
        if (IsPublishedPackage(project.StoragePath))
        {
            var dllName = project.Name + ".dll";
            // Try to find the project's main dll
            var dlls = Directory.GetFiles(project.StoragePath, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(f => f != null && !f!.StartsWith("System.") && !f!.StartsWith("Microsoft.") && !f!.StartsWith("SQLite") && f != "hostfxr.dll" && f != "hostpolicy.dll")
                .ToArray();
            
            if (dlls.Length > 0)
            {
                var mainDll = dlls.FirstOrDefault(d => d == dllName) ?? dlls[0];
                var extraArgs = string.IsNullOrWhiteSpace(project.StartupArgs) 
                    ? $"--urls http://0.0.0.0:{port}"
                    : $"{project.StartupArgs}";
                
                // Check if user already included --urls
                if (!extraArgs.Contains("--urls"))
                    extraArgs = $"--urls http://0.0.0.0:{port} " + extraArgs;
                
                return (dotnet, $"{mainDll} {extraArgs}");
            }
        }
        
        // Source project: dotnet run
        var defaultArgs = $"--urls http://0.0.0.0:{project.Port}";
        var args2 = !string.IsNullOrWhiteSpace(project.StartupArgs)
            ? $"run -c Release {project.StartupArgs}"
            : $"run -c Release {defaultArgs}";
        
        if (!args2.Contains("--urls"))
            args2 = $"run -c Release {defaultArgs} " + args2.Replace("run -c Release ", "");
        
        return (dotnet, args2);
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
        OwnerName = p.User?.Username,
        StartupArgs = p.StartupArgs,
        StoragePath = p.StoragePath
    };

    public List<FileNode> GetFileTree(string rootPath)
    {
        var result = new List<FileNode>();
        if (!Directory.Exists(rootPath)) return result;
        
        foreach (var dir in Directory.GetDirectories(rootPath))
        {
            var info = new DirectoryInfo(dir);
            if (info.Name == ".git" || info.Name == "bin" || info.Name == "obj") continue;
            result.Add(new FileNode
            {
                Name = info.Name,
                Path = dir.Replace(rootPath, "").TrimStart(Path.DirectorySeparatorChar),
                IsDirectory = true,
                Size = 0,
                Children = GetFileTree(dir)
            });
        }
        foreach (var file in Directory.GetFiles(rootPath))
        {
            var info = new FileInfo(file);
            result.Add(new FileNode
            {
                Name = info.Name,
                Path = file.Replace(rootPath, "").TrimStart(Path.DirectorySeparatorChar),
                IsDirectory = false,
                Size = info.Length,
                Children = null
            });
        }
        return result.OrderBy(f => f.IsDirectory ? 0 : 1).ThenBy(f => f.Name).ToList();
    }
}
