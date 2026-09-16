namespace DocuChat.Application;

public interface IDocumentService
{
    Task<DocumentDto> UploadAsync(Guid userId, UploadDocumentCommand command, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentDto>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid documentId, CancellationToken cancellationToken);
}
