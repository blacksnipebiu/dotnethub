
using System.ComponentModel.DataAnnotations;

namespace DotNetHub.Server.Models;

public class Project
{
    [Key]
    public int Id { get; set; }
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";
    
    [MaxLength(500)]
    public string Description { get; set; } = "";
    
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public int Port { get; set; }
    
    [MaxLength(50)]
    public string Status { get; set; } = "stopped"; // stopped, running, building, error
    
    public int? ProcessId { get; set; }
    
    public string DotNetVersion { get; set; } = "8.0";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public string StoragePath { get; set; } = "";
    
    [MaxLength(200)]
    public string? GitRepo { get; set; }
    
    public bool IsPublic { get; set; } = false;

    [MaxLength(500)]
    public string StartupArgs { get; set; } = "";  // custom dotnet run args, e.g. "--urls http://0.0.0.0:5000"
}

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Port { get; set; }
    public string Status { get; set; } = "stopped";
    public string DotNetVersion { get; set; } = "8.0";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsPublic { get; set; }
    public string? GitRepo { get; set; }
    public string? OwnerName { get; set; }
    public string StartupArgs { get; set; } = "";
    public string StoragePath { get; set; } = "";
}

public class ProjectCreateRequest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Port { get; set; } = 5000;
    public string DotNetVersion { get; set; } = "8.0";
    public bool IsPublic { get; set; } = false;
    public string? GitRepo { get; set; }
}

public class ProjectUpdateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? Port { get; set; }
    public bool? IsPublic { get; set; }
    public string? GitRepo { get; set; }
    public string? StartupArgs { get; set; }
}

public class FileNode
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public List<FileNode>? Children { get; set; }
}
