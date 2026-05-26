
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DotNetHub.Server.Data;
using DotNetHub.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
var dbPath = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=/data/dotnethub.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(dbPath));

// JWT Auth
var jwtKey = builder.Configuration["Jwt:Key"] ?? "DotNetHub_Super_Secret_Key_Min_32_Chars!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "DotNetHub",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "DotNetHub",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProjectService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers();

var app = builder.Build();

// Auto-migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Ensure storage directory
var storagePath = builder.Configuration["Storage:ProjectsPath"] ?? "/data/projects";
Directory.CreateDirectory(storagePath);

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Serve frontend SPA — check wwwroot first (Docker), then relative path (dev)
var frontendRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(frontendRoot))
    frontendRoot = Path.Combine(builder.Environment.ContentRootPath, "..", "frontend", "dist");

if (Directory.Exists(frontendRoot))
{
    var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendRoot);
    
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
    
    // SPA fallback — all non-API routes go to index.html
    app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });
}

app.Run();
