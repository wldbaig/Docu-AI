using DocuChat.Application;

namespace DocuChat.Tests;

internal sealed class FakeEmbeddingProvider(string name, float value) : IEmbeddingModelProvider
{
    public string Name => name;

    public Task<IReadOnlyList<float[]>> CreateAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<float[]>>(inputs.Select(_ => new[] { value }).ToArray());
}
