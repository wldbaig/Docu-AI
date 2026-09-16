namespace DocuChat.Domain;

public sealed class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public required string FileName { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public DocumentStatus Status { get; set; } = DocumentStatus.Processing;
    public string? Error { get; set; }
    public List<Chunk> Chunks { get; set; } = [];
}
