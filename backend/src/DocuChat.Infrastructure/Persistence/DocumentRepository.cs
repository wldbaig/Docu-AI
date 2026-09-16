using DocuChat.Application;
using DocuChat.Domain;
using Microsoft.EntityFrameworkCore;

namespace DocuChat.Infrastructure.Persistence;

public sealed class DocumentRepository(AppDbContext db) : IDocumentRepository
{
    public void Add(Document document) => db.Documents.Add(document);

    public void Remove(Document document) => db.Documents.Remove(document);

    public Task<Document?> FindOwnedAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken) =>
        db.Documents.SingleOrDefaultAsync(
            x => x.Id == documentId && x.UserId == userId,
            cancellationToken);

    public async Task<IReadOnlyList<Document>> ListOwnedAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await db.Documents.AsNoTracking()
            .Include(x => x.Chunks)
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

    public Task<bool> HasReadyAsync(
        Guid userId,
        IReadOnlyList<Guid>? documentIds,
        CancellationToken cancellationToken) =>
        db.Documents.AsNoTracking().AnyAsync(
            x => x.UserId == userId &&
                 x.Status == DocumentStatus.Ready &&
                 (documentIds == null || documentIds.Count == 0 || documentIds.Contains(x.Id)),
            cancellationToken);
}
