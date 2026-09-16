namespace DocuChat.Application;

public sealed record SourceDto(
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    string Content,
    double Score);
