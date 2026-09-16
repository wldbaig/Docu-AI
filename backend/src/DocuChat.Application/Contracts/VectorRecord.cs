namespace DocuChat.Application;

public sealed record VectorRecord(
    Guid ChunkId,
    Guid UserId,
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    string Content,
    float[] Vector);
