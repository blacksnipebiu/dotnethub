
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DotNetHub.Server.Data;
using DotNetHub.Server.Services;

// Register code pages for GBK support (Chinese zip archives)
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Increase request size limit for file uploads (default is 30 MB)
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 300_000_000); // 300 MB
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 300_000_000; // 300 MB
});

// Database
var dbConnectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=dotnethub.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(dbConnectionString));

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
builder.Services.AddScoped<ChunkUploadService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

var app = builder.Build();

// ---- Ensure directories exist before using them ----

// Get database path from connection string, extract the file path
var dbDataSource = dbConnectionString.Replace("Data Source=", "").Trim();
if (!Path.IsPathRooted(dbDataSource))
    dbDataSource = Path.Combine(app.Environment.ContentRootPath, dbDataSource);

var dbDir = Path.GetDirectoryName(dbDataSource);
if (dbDir != null) Directory.CreateDirectory(dbDir);

// Storage directory for deployed projects
var storagePath = builder.Configuration["Storage:ProjectsPath"] ?? "projects";
if (!Path.IsPathRooted(storagePath))
    storagePath = Path.Combine(app.Environment.ContentRootPath, storagePath);
Directory.CreateDirectory(storagePath);

// Auto-migrate database — recreate if schema changed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch
    {
        // Schema mismatch — delete and recreate
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Serve frontend SPA — check wwwroot first (Docker / publish), then dev path
var frontendRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(frontendRoot))
    frontendRoot = Path.Combine(app.Environment.ContentRootPath, "..", "frontend", "dist");

if (Directory.Exists(frontendRoot))
{
    var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendRoot);

    // Serve static files globally first (CSS, JS, images, etc.)
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });

    // SPA fallback: only for non-API routes that don't match a static file
    app.MapWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), spa =>
    {
        spa.Run(async ctx =>
        {
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.SendFileAsync(Path.Combine(frontendRoot, "index.html"));
        });
    });
}

app.Run();
