using System.Runtime.CompilerServices;
using DocuChat.Application;
using Microsoft.Extensions.Options;

namespace DocuChat.Infrastructure.AI;

public sealed class ConfigurableChatCompletionService(
    IEnumerable<IChatModelProvider> providers,
    IOptions<AiOptions> options) : IChatCompletionService
{
    private readonly IReadOnlyDictionary<string, IChatModelProvider> _providers =
        providers.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    private readonly string _selected = options.Value.ChatProvider;

    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_providers.TryGetValue(_selected, out var provider))
        {
            throw new ExternalServiceException(
                $"Unknown chat provider '{_selected}'. Available providers: {string.Join(", ", _providers.Keys)}.");
        }

        await foreach (var token in provider.StreamAsync(systemPrompt, userPrompt, cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            yield return token;
        }
    }
}
