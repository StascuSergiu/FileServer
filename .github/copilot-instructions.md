# Copilot Instructions for FileServer

## Project Overview
This is a simple in-memory file server built with ASP.NET Core (.NET 8/9) for uploading and downloading files via Base64.

## Tech Stack
- C# / .NET 8+
- ASP.NET Core Minimal APIs
- Hangfire (in-memory storage)
- Swagger / Swashbuckle
- Docker

## Code Style Guidelines

### General
- Use C# 12 features (file-scoped namespaces, records, primary constructors)
- Prefer `var` for local variables when type is obvious
- Use meaningful, descriptive names for variables and methods
- Keep methods small and focused (single responsibility)

### API Endpoints
- Use Minimal APIs pattern (not controllers)
- Return appropriate HTTP status codes (200, 201, 400, 404)
- Use DTOs (records) for request/response models
- Add `.WithName()` for endpoint identification

### Dependency Injection
- Register services in `Program.cs`
- Use interfaces for services (e.g., `IFileStorageService`)
- Prefer `AddSingleton` for stateful services, `AddTransient` for stateless

### Configuration
- Use `IOptions<T>` pattern for configuration
- Store settings in `appsettings.json`
- Create strongly-typed settings classes

### Error Handling
- Return `ErrorResponse` record for error messages
- Use `Results.BadRequest()`, `Results.NotFound()` for errors
- Validate input before processing

### Async/Await
- Use async methods for I/O operations
- Suffix async methods with `Async`

## File Structure
```
/
├── Models/          # DTOs and domain models
├── Services/        # Business logic services
├── Jobs/            # Hangfire background jobs
├── Program.cs       # Application entry point
├── appsettings.json # Configuration
└── Dockerfile       # Container support
```

## Don't
- Don't use controllers (use Minimal APIs)
- Don't store files on disk (use in-memory only)
- Don't add authentication (public API)
- Don't use Entity Framework (simple dictionary storage)
