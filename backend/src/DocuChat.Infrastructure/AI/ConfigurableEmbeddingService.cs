using DocuChat.Application;
using Microsoft.Extensions.Options;

namespace DocuChat.Infrastructure.AI;

public sealed class ConfigurableEmbeddingService(
    IEnumerable<IEmbeddingModelProvider> providers,
    IOptions<AiOptions> options) : IEmbeddingService
{
    private readonly IReadOnlyDictionary<string, IEmbeddingModelProvider> _providers =
        providers.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    private readonly string _selected = options.Value.EmbeddingProvider;

    public Task<IReadOnlyList<float[]>> CreateAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken)
    {
        if (!_providers.TryGetValue(_selected, out var provider))
        {
            throw new ExternalServiceException(
                $"Unknown embedding provider '{_selected}'. Available providers: {string.Join(", ", _providers.Keys)}.");
        }

        return provider.CreateAsync(inputs, cancellationToken);
    }
}
