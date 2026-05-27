
using Microsoft.EntityFrameworkCore;
using DotNetHub.Server.Models;

namespace DotNetHub.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
            
        modelBuilder.Entity<Project>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId);
            
        // Seed default admin
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = "admin",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Seed system settings
        modelBuilder.Entity<SystemSetting>().HasData(
            new SystemSetting { Key = "MaxUploadSizeMB", Value = "300", Description = "最大上传文件大小 (MB)" },
            new SystemSetting { Key = "DefaultPort", Value = "5000", Description = "默认端口号" }
        );
    }
}
