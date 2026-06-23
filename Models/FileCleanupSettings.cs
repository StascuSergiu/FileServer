namespace FileServer.Models;

public class FileCleanupSettings
{
    public const string SectionName = "FileCleanup";
    
    public int RunEveryMinutes { get; set; } = 5;
    public int DeleteOlderThanMinutes { get; set; } = 30;
    public int MaxFileSizeMB { get; set; } = 10;
    public int MaxTotalStorageMB { get; set; } = 100;
    public int TimezoneOffsetHours { get; set; } = 0;
}
