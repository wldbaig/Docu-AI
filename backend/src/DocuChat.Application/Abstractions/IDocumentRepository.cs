using DocuChat.Domain;

namespace DocuChat.Application;

public interface IDocumentRepository
{
    void Add(Document document);
    void Remove(Document document);
    Task<Document?> FindOwnedAsync(Guid userId, Guid documentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Document>> ListOwnedAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> HasReadyAsync(Guid userId, IReadOnlyList<Guid>? documentIds, CancellationToken cancellationToken);
}
