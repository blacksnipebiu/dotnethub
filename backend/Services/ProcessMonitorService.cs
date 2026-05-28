
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
        _logger.LogInformation("[Monitor] started, interval=10s");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
                var runningProjects = db.Projects.Where(p => p.Status == "running" && p.ProcessId != null).ToList();
                _logger.LogDebug("[Monitor] checking {Count} running projects", runningProjects.Count);

                foreach (var project in runningProjects)
                {
                    bool exited = false;
                    try
                    {
                        using var proc = System.Diagnostics.Process.GetProcessById(project.ProcessId!.Value);
                        exited = proc.HasExited;
                        _logger.LogDebug("[Monitor] project {Id} PID={Pid} HasExited={Exited}", project.Id, project.ProcessId, exited);
                    }
                    catch (InvalidOperationException)
                    {
                        _logger.LogInformation("[Monitor] project {Id} PID {Pid}: process not found (InvalidOpExc)", project.Id, project.ProcessId);
                        exited = true;
                    }
                    catch (ArgumentException)
                    {
                        _logger.LogInformation("[Monitor] project {Id} PID {Pid}: process not found (ArgExc)", project.Id, project.ProcessId);
                        exited = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[Monitor] project {Id} PID {Pid}: check error", project.Id, project.ProcessId);
                    }

                    if (exited)
                    {
                        _logger.LogInformation("[Monitor] project {Id} ({Name}) marked stopped (PID={Pid})", project.Id, project.Name, project.ProcessId);
                        project.Status = "stopped";
                        project.ProcessId = null;
                        project.ActualCommand = null;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Monitor] outer loop error");
            }

            await Task.Delay(10000, stoppingToken);
        }
    }
}
