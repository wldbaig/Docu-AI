namespace DocuChat.Domain;

public sealed class Chunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public int Index { get; set; }
    public required string Content { get; set; }
}
