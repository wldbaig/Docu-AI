namespace DocuChat.Application;

public interface IEmbeddingService
{
    Task<IReadOnlyList<float[]>> CreateAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken);
}
