using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DocuChat.Application;
using Microsoft.Extensions.Options;

namespace DocuChat.Infrastructure.AI;

public sealed class ClaudeChatProvider(IHttpClientFactory clients, IOptions<AiOptions> options) : IChatModelProvider
{
    private readonly ModelProviderOptions _settings = options.Value.Provider("Claude");
    public string Name => "Claude";

    public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userPrompt, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ProviderHttp.EnsureConfigured(_settings, Name);
        using var request = new HttpRequestMessage(HttpMethod.Post, ProviderHttp.Endpoint(_settings, "messages"));
        request.Headers.Add("x-api-key", _settings.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = JsonContent.Create(new
        {
            model = _settings.ChatModel,
            max_tokens = 2048,
            stream = true,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userPrompt } }
        });
        using var response = await clients.CreateClient("ai-providers").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await ProviderHttp.EnsureSuccessAsync(response, Name, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
            var data = line[5..].TrimStart();
            if (data.Length == 0) continue;
            using var json = JsonDocument.Parse(data);
            var root = json.RootElement;
            var typeName = root.TryGetProperty("type", out var type) ? type.GetString() : null;
            if (typeName == "content_block_delta" &&
                root.TryGetProperty("delta", out var delta) && delta.TryGetProperty("type", out var deltaType) &&
                deltaType.GetString() == "text_delta" && delta.TryGetProperty("text", out var text))
                yield return text.GetString() ?? "";
            if (typeName == "error" && root.TryGetProperty("error", out var error))
                throw new ExternalServiceException($"Claude stream failed: {error.GetProperty("message").GetString()}");
        }
    }
}
