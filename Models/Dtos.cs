namespace FileServer.Models;

public record UploadRequest(string Base64Content);

public record UploadResponse(Guid Id);

public record StatsResponse(int TotalFiles, long TotalSizeBytes, double TotalSizeMB);

public record FileInfoResponse(
    Guid FileId,
    DateTime CreatedAt,
    int TimeUntilExpirationMinutes,
    double FileSizeMB);

public record ErrorResponse(string Message);
