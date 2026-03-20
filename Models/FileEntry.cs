namespace FileServer.Models;

public class FileEntry
{
    public Guid Id { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAtUtc { get; set; }
}
