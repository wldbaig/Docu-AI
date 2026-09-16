namespace DocuChat.Application;

public interface IEmbeddingModelProvider
{
    string Name { get; }
    Task<IReadOnlyList<float[]>> CreateAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken);
}
