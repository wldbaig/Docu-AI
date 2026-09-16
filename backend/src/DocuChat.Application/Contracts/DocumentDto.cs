namespace DocuChat.Application;

public sealed record DocumentDto(
    Guid Id,
    string FileName,
    DateTimeOffset UploadedAt,
    string Status,
    string? Error,
    int ChunkCount);
