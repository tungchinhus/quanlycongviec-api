using System;
using System.IO;
using System.Threading.Tasks;

namespace quanlyfilesBE.Services;

public interface IFileLoggerService
{
    Task LogErrorAsync(string message, Exception? exception = null, string? additionalInfo = null);
    Task LogInfoAsync(string message);
    Task LogWarningAsync(string message);
}

public class FileLoggerService : IFileLoggerService
{
    private readonly string _logDirectory;
    private readonly object _lockObject = new object();

    public FileLoggerService()
    {
        _logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    private string GetLogFilePath(string logType = "error")
    {
        var fileName = $"{logType}-{DateTime.Now:yyyy-MM-dd}.log";
        return Path.Combine(_logDirectory, fileName);
    }

    private async Task WriteLogAsync(string level, string message, Exception? exception = null, string? additionalInfo = null)
    {
        var logEntry = new System.Text.StringBuilder();
        logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}]");
        logEntry.AppendLine($"Message: {message}");
        
        if (!string.IsNullOrEmpty(additionalInfo))
        {
            logEntry.AppendLine($"Additional Info: {additionalInfo}");
        }
        
        if (exception != null)
        {
            logEntry.AppendLine($"Exception Type: {exception.GetType().FullName}");
            logEntry.AppendLine($"Exception Message: {exception.Message}");
            
            if (exception.InnerException != null)
            {
                logEntry.AppendLine($"Inner Exception Type: {exception.InnerException.GetType().FullName}");
                logEntry.AppendLine($"Inner Exception Message: {exception.InnerException.Message}");
                logEntry.AppendLine($"Inner Exception Stack Trace: {exception.InnerException.StackTrace}");
            }
            
            logEntry.AppendLine($"Stack Trace: {exception.StackTrace}");
        }
        
        logEntry.AppendLine(new string('-', 80));
        logEntry.AppendLine();

        var logFilePath = GetLogFilePath(level.ToLower());
        
        lock (_lockObject)
        {
            try
            {
                File.AppendAllText(logFilePath, logEntry.ToString());
            }
            catch (Exception ex)
            {
                // Fallback to console if file write fails
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
                Console.WriteLine(logEntry.ToString());
            }
        }
        
        await Task.CompletedTask;
    }

    public async Task LogErrorAsync(string message, Exception? exception = null, string? additionalInfo = null)
    {
        await WriteLogAsync("ERROR", message, exception, additionalInfo);
    }

    public async Task LogInfoAsync(string message)
    {
        await WriteLogAsync("INFO", message);
    }

    public async Task LogWarningAsync(string message)
    {
        await WriteLogAsync("WARNING", message);
    }
}

