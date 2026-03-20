using FileServer.Jobs;
using FileServer.Models;
using FileServer.Services;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<FileCleanupSettings>(
    builder.Configuration.GetSection(FileCleanupSettings.SectionName));

// Services
builder.Services.AddSingleton<IFileStorageService, InMemoryFileStorageService>();
builder.Services.AddTransient<FileCleanupJob>();

// Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());

builder.Services.AddHangfireServer();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FileServer API", Version = "v1" });
});

var app = builder.Build();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileServer API v1"));

// Redirect root to Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Hangfire Dashboard (no auth)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AllowAllDashboardAuthorizationFilter() }
});

// Schedule cleanup job
var settings = app.Services.GetRequiredService<IOptions<FileCleanupSettings>>().Value;
RecurringJob.AddOrUpdate<FileCleanupJob>(
    "file-cleanup",
    job => job.Execute(),
    $"*/{settings.RunEveryMinutes} * * * *");

// Upload endpoint
app.MapPost("/api/files", (UploadRequest request, IFileStorageService storage, IOptions<FileCleanupSettings> options) =>
{
    // Validate base64
    byte[] content;
    try
    {
        content = Convert.FromBase64String(request.Base64Content);
    }
    catch (FormatException)
    {
        return Results.BadRequest(new ErrorResponse("Invalid Base64 content."));
    }

    // Check file size
    var maxSizeBytes = options.Value.MaxFileSizeMB * 1024 * 1024;
    if (content.Length > maxSizeBytes)
    {
        return Results.BadRequest(new ErrorResponse($"File size exceeds maximum allowed size of {options.Value.MaxFileSizeMB} MB."));
    }

    var id = storage.Store(content);
    return Results.Created($"/api/files/{id}", new UploadResponse(id));
})
.WithName("UploadFile")
.WithSummary("Upload file as Base64")
.WithDescription("Upload a file by sending its content as a Base64-encoded string. Returns a GUID identifier for the uploaded file.");

// Upload file endpoint (multipart form)
app.MapPost("/api/files/upload", async (IFormFile file, IFileStorageService storage, IOptions<FileCleanupSettings> options) =>
{
    if (file is null || file.Length == 0)
    {
        return Results.BadRequest(new ErrorResponse("No file provided."));
    }

    // Check file size
    var maxSizeBytes = options.Value.MaxFileSizeMB * 1024 * 1024;
    if (file.Length > maxSizeBytes)
    {
        return Results.BadRequest(new ErrorResponse($"File size exceeds maximum allowed size of {options.Value.MaxFileSizeMB} MB."));
    }

    using var memoryStream = new MemoryStream();
    await file.CopyToAsync(memoryStream);
    var content = memoryStream.ToArray();

    var id = storage.Store(content);
    return Results.Created($"/api/files/{id}", new UploadResponse(id));
})
.WithName("UploadFileForm")
.WithSummary("Upload file (multipart form)")
.WithDescription("Upload a file using multipart/form-data. Send the file in the 'file' field. Returns a GUID identifier for the uploaded file.")
.DisableAntiforgery();

// Download endpoint
app.MapGet("/api/files/{id:guid}", (Guid id, IFileStorageService storage) =>
{
    var file = storage.Get(id);
    if (file is null)
    {
        return Results.NotFound(new ErrorResponse("File not found."));
    }

    return Results.File(
        file.Content,
        contentType: "application/octet-stream",
        fileDownloadName: id.ToString());
})
.WithName("DownloadFile")
.WithSummary("Download file by ID")
.WithDescription("Download a file by its GUID identifier. Returns the file as application/octet-stream with Content-Disposition header for automatic download.");

// Stats endpoint
app.MapGet("/api/files/stats", (IFileStorageService storage) =>
{
    var (totalFiles, totalSizeBytes) = storage.GetStats();
    var totalSizeMB = Math.Round(totalSizeBytes / (1024.0 * 1024.0), 2);
    return Results.Ok(new StatsResponse(totalFiles, totalSizeBytes, totalSizeMB));
})
.WithName("GetStats")
.WithSummary("Get storage statistics")
.WithDescription("Returns the total number of files stored and total memory usage in bytes and megabytes.");

// List files endpoint
app.MapGet("/api/files/list", (IFileStorageService storage, IOptions<FileCleanupSettings> options) =>
{
    var expirationMinutes = options.Value.DeleteOlderThanMinutes;
    var timezoneOffset = TimeSpan.FromHours(options.Value.TimezoneOffsetHours);
    var files = storage.GetAll().Select(f =>
    {
        var ageMinutes = (int)(DateTime.UtcNow - f.CreatedAtUtc).TotalMinutes;
        var timeUntilExpiration = Math.Max(0, expirationMinutes - ageMinutes);
        var fileSizeMB = Math.Round(f.Content.Length / (1024.0 * 1024.0), 4);
        var createdAt = f.CreatedAtUtc + timezoneOffset;
        return new FileInfoResponse(
            f.Id,
            createdAt,
            timeUntilExpiration,
            fileSizeMB);
    }).ToList();
    
    return Results.Ok(files);
})
.WithName("ListFiles")
.WithSummary("List all files")
.WithDescription("Returns a list of all stored files with their ID, creation date, time until expiration, and file size.");

app.Run();

// Hangfire dashboard authorization filter (allow all)
public class AllowAllDashboardAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}
