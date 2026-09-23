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
    private readonly string _default = options.Value.ChatProvider;

    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userPrompt,
        string? provider,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var requested = string.IsNullOrWhiteSpace(provider) ? _default : provider;

        if (!_providers.TryGetValue(requested, out var selected))
        {
            // A caller-supplied provider that we don't recognise is a bad request;
            // a misconfigured default is a server-side configuration error.
            var message = $"Unknown chat provider '{requested}'. Available providers: {string.Join(", ", _providers.Keys)}.";
            throw string.IsNullOrWhiteSpace(provider)
                ? new ExternalServiceException(message)
                : new ValidationException(message);
        }

        await foreach (var token in selected.StreamAsync(systemPrompt, userPrompt, cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            yield return token;
        }
    }
}
