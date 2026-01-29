using Microsoft.Extensions.Options;

namespace syncToServer;

/// <summary>
/// Background worker that runs the sync operation on a periodic schedule.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly SyncSettings _settings;
    private readonly SyncService _syncService;

    public Worker(
        ILogger<Worker> logger,
        IOptions<SyncSettings> settings,
        SyncService syncService)
    {
        _logger = logger;
        _settings = settings.Value;
        _syncService = syncService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started at: {Time}", DateTimeOffset.Now);

        // Validate configuration
        if (!ValidateConfiguration())
        {
            _logger.LogError("Configuration validation failed. Worker will exit.");
            return;
        }

        // Use PeriodicTimer for efficient periodic execution
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.IntervalMinutes));

        // Perform initial sync immediately
        _logger.LogInformation("Performing initial sync...");
        await _syncService.SyncAsync();

        // Watch source folder: khi có file mới/thay đổi thì sync sau 30 giây (tránh sync liên tục khi copy nhiều file)
        StartFileWatcher(stoppingToken);

        // Then sync on schedule
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Waiting for next sync interval ({IntervalMinutes} minutes)...", _settings.IntervalMinutes);
                
                // Wait for the next interval or cancellation
                bool timerElapsed = await timer.WaitForNextTickAsync(stoppingToken);
                
                if (timerElapsed && !stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Starting scheduled sync at: {Time}", DateTimeOffset.Now);
                    await _syncService.SyncAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in worker loop");
                // Continue running even if one sync fails
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Worker stopped at: {Time}", DateTimeOffset.Now);
    }

    private bool ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_settings.SourcePath))
        {
            _logger.LogError("SourcePath is not configured in appsettings.json");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.DestServer))
        {
            _logger.LogError("DestServer is not configured in appsettings.json");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.NetworkCredentials.Username))
        {
            _logger.LogError("NetworkCredentials.Username is not configured in appsettings.json");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.NetworkCredentials.Password))
        {
            _logger.LogError("NetworkCredentials.Password is not configured in appsettings.json");
            return false;
        }

        if (_settings.IntervalMinutes <= 0)
        {
            _logger.LogError("IntervalMinutes must be greater than 0");
            return false;
        }

        return true;
    }

    private void StartFileWatcher(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.SourcePath) || !Directory.Exists(_settings.SourcePath))
            return;

        var watcher = new FileSystemWatcher(_settings.SourcePath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };

        CancellationTokenSource? debounceCts = null;
        var lockObj = new object();
        const int debounceSeconds = 30;

        void OnChange()
        {
            lock (lockObj)
            {
                debounceCts?.Cancel();
                debounceCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var cts = debounceCts;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(debounceSeconds), cts.Token);
                        if (stoppingToken.IsCancellationRequested) return;
                        _logger.LogInformation("File change detected, starting sync...");
                        await _syncService.SyncAsync();
                    }
                    catch (OperationCanceledException) { }
                }, cts.Token);
            }
        }

        watcher.Created += (_, _) => OnChange();
        watcher.Changed += (_, _) => OnChange();
        watcher.Renamed += (_, _) => OnChange();
        watcher.EnableRaisingEvents = true;
        _logger.LogInformation("Watching source folder for changes (sync after {Sec}s when files change)", debounceSeconds);
    }
}
