using System.Collections.Concurrent;
using FileServer.Models;

namespace FileServer.Services;

public interface IFileStorageService
{
    Guid Store(byte[] content);
    FileEntry? Get(Guid id);
    IEnumerable<FileEntry> GetAll();
    bool Delete(Guid id);
    int DeleteOlderThan(TimeSpan age);
    (int totalFiles, long totalSizeBytes) GetStats();
}

public class InMemoryFileStorageService : IFileStorageService
{
    private readonly ConcurrentDictionary<Guid, FileEntry> _files = new();

    public Guid Store(byte[] content)
    {
        var entry = new FileEntry
        {
            Id = Guid.NewGuid(),
            Content = content,
            CreatedAtUtc = DateTime.UtcNow
        };

        _files[entry.Id] = entry;
        return entry.Id;
    }

    public FileEntry? Get(Guid id)
    {
        return _files.TryGetValue(id, out var entry) ? entry : null;
    }

    public IEnumerable<FileEntry> GetAll()
    {
        return _files.Values.ToList();
    }

    public bool Delete(Guid id)
    {
        return _files.TryRemove(id, out _);
    }

    public int DeleteOlderThan(TimeSpan age)
    {
        var threshold = DateTime.UtcNow - age;
        var toDelete = _files.Where(kvp => kvp.Value.CreatedAtUtc < threshold)
                             .Select(kvp => kvp.Key)
                             .ToList();

        foreach (var id in toDelete)
        {
            _files.TryRemove(id, out _);
        }

        return toDelete.Count;
    }

    public (int totalFiles, long totalSizeBytes) GetStats()
    {
        var files = _files.Values.ToList();
        var totalFiles = files.Count;
        var totalSizeBytes = files.Sum(f => (long)f.Content.Length);
        return (totalFiles, totalSizeBytes);
    }
}
