using DocuChat.Application;
using Microsoft.Extensions.Options;

namespace DocuChat.Infrastructure.AI;

public sealed class ProviderCatalog(
    IEnumerable<IChatModelProvider> chatProviders,
    IEnumerable<IEmbeddingModelProvider> embeddingProviders,
    IOptions<AiOptions> options) : IProviderCatalog
{
    private readonly AiOptions _ai = options.Value;
    private readonly string[] _chatNames = chatProviders.Select(x => x.Name).ToArray();
    private readonly string[] _embeddingNames = embeddingProviders.Select(x => x.Name).ToArray();

    public ProviderCatalogDto Get()
    {
        var chat = _chatNames.Select(name => new ProviderInfo(name, IsConfigured(name, embedding: false))).ToArray();
        var embedding = _embeddingNames.Select(name => new ProviderInfo(name, IsConfigured(name, embedding: true))).ToArray();
        return new ProviderCatalogDto(chat, embedding, _ai.ChatProvider, _ai.EmbeddingProvider);
    }

    private bool IsConfigured(string name, bool embedding)
    {
        if (!_ai.Providers.TryGetValue(name, out var provider)) return false;
        var model = embedding ? provider.EmbeddingModel : provider.ChatModel;
        return !string.IsNullOrWhiteSpace(provider.ApiKey)
               && !string.IsNullOrWhiteSpace(provider.BaseUrl)
               && !string.IsNullOrWhiteSpace(model);
    }
}
