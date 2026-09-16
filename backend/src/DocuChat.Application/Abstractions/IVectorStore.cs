namespace DocuChat.Application;

public interface IVectorStore
{
    Task UpsertAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        Guid userId,
        float[] queryVector,
        int topK,
        IReadOnlyList<Guid>? documentIds,
        CancellationToken cancellationToken);
    Task DeleteDocumentAsync(Guid userId, Guid documentId, CancellationToken cancellationToken);
}
