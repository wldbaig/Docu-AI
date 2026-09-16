using DocuChat.Domain;

namespace DocuChat.Application;

public sealed class DocumentService(
    IDocumentRepository documents,
    IUnitOfWork unitOfWork,
    ITextExtractor extractor,
    ITextChunker chunker,
    IEmbeddingService embeddings,
    IVectorStore vectorStore) : IDocumentService
{
    private const long MaxFileBytes = 10 * 1024 * 1024;

    public async Task<DocumentDto> UploadAsync(Guid userId, UploadDocumentCommand command, CancellationToken cancellationToken)
    {
        if (command.Length is <= 0 or > MaxFileBytes) throw new ValidationException("File must be between 1 byte and 10 MB.");
        var extension = Path.GetExtension(command.FileName).ToLowerInvariant();
        if (extension is not ".pdf" and not ".txt") throw new ValidationException("Only PDF and TXT files are supported.");

        var document = new Document { UserId = userId, FileName = Path.GetFileName(command.FileName), Status = DocumentStatus.Processing };
        documents.Add(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        try
        {
            var text = await extractor.ExtractAsync(command.Content, extension, cancellationToken);
            var parts = chunker.Split(text);
            if (parts.Count == 0) throw new ValidationException("No readable text was found in the document.");

            for (var offset = 0; offset < parts.Count; offset += 32)
            {
                var batch = parts.Skip(offset).Take(32).ToArray();
                var vectors = await embeddings.CreateAsync(batch, cancellationToken);
                if (vectors.Count != batch.Length) throw new ExternalServiceException("Embedding count did not match the chunk count.");
                var records = new List<VectorRecord>(batch.Length);
                for (var i = 0; i < batch.Length; i++)
                {
                    var chunk = new Chunk { Index = offset + i, Content = batch[i] };
                    document.Chunks.Add(chunk);
                    records.Add(new VectorRecord(chunk.Id, userId, document.Id, document.FileName, chunk.Index, chunk.Content, vectors[i]));
                }
                await vectorStore.UpsertAsync(records, cancellationToken);
            }
            document.Status = DocumentStatus.Ready;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Map(document);
        }
        catch (Exception ex)
        {
            try { await vectorStore.DeleteDocumentAsync(userId, document.Id, CancellationToken.None); } catch { /* preserve the indexing failure */ }
            document.Chunks.Clear();
            document.Status = DocumentStatus.Failed;
            document.Error = ex.Message[..Math.Min(ex.Message.Length, 500)];
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<DocumentDto>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        (await documents.ListOwnedAsync(userId, cancellationToken)).OrderByDescending(x => x.UploadedAt).Select(Map).ToArray();

    public async Task DeleteAsync(Guid userId, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await documents.FindOwnedAsync(userId, documentId, cancellationToken)
            ?? throw new NotFoundException("Document not found.");
        await vectorStore.DeleteDocumentAsync(userId, documentId, cancellationToken);
        documents.Remove(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static DocumentDto Map(Document document) =>
        new(document.Id, document.FileName, document.UploadedAt, document.Status.ToString(), document.Error, document.Chunks.Count);
}
