namespace DocuChat.Application;

public interface IRagService
{
    Task<RagContext> PrepareAsync(Guid userId, AskRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<string> StreamAnswerAsync(Guid userId, RagContext context, CancellationToken cancellationToken);
}
