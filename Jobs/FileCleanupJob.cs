using FileServer.Models;
using FileServer.Services;
using Microsoft.Extensions.Options;

namespace FileServer.Jobs;

public class FileCleanupJob
{
    private readonly IFileStorageService _fileStorage;
    private readonly FileCleanupSettings _settings;
    private readonly ILogger<FileCleanupJob> _logger;

    public FileCleanupJob(
        IFileStorageService fileStorage,
        IOptions<FileCleanupSettings> settings,
        ILogger<FileCleanupJob> logger)
    {
        _fileStorage = fileStorage;
        _settings = settings.Value;
        _logger = logger;
    }

    public void Execute()
    {
        var age = TimeSpan.FromMinutes(_settings.DeleteOlderThanMinutes);
        var deletedCount = _fileStorage.DeleteOlderThan(age);
        
        _logger.LogInformation(
            "File cleanup completed. Deleted {Count} files older than {Minutes} minutes.",
            deletedCount,
            _settings.DeleteOlderThanMinutes);
    }
}
