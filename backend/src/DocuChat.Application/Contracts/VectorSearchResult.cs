namespace DocuChat.Application;

public sealed record VectorSearchResult(
    Guid ChunkId,
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    string Content,
    double Score);
