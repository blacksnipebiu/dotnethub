
namespace DotNetHub.Server.Services;

public class ProcessMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProcessMonitorService> _logger;

    public ProcessMonitorService(IServiceScopeFactory scopeFactory, ILogger<ProcessMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProcessMonitor started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
                var runningProjects = db.Projects.Where(p => p.Status == "running" && p.ProcessId != null).ToList();

                foreach (var project in runningProjects)
                {
                    try
                    {
                        var proc = System.Diagnostics.Process.GetProcessById(project.ProcessId!.Value);
                        if (proc.HasExited)
                        {
                            _logger.LogInformation("ProcessMonitor: project {Id} ({Name}) exited, updating status", project.Id, project.Name);
                            project.Status = "stopped";
                            project.ProcessId = null;
                            project.ActualCommand = null;
                            await db.SaveChangesAsync(stoppingToken);
                            proc.Dispose();
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Process not found — already exited
                        _logger.LogInformation("ProcessMonitor: project {Id} PID {Pid} not found, marking stopped", project.Id, project.ProcessId);
                        project.Status = "stopped";
                        project.ProcessId = null;
                        project.ActualCommand = null;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "ProcessMonitor: error checking project {Id}", project.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessMonitor: loop error");
            }

            await Task.Delay(10000, stoppingToken); // Check every 10s
        }
    }
}
