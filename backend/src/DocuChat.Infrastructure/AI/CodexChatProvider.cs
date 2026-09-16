using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DocuChat.Application;
using Microsoft.Extensions.Options;

namespace DocuChat.Infrastructure.AI;

public sealed class CodexChatProvider(IHttpClientFactory clients, IOptions<AiOptions> options) : IChatModelProvider
{
    private readonly ModelProviderOptions _settings = options.Value.Provider("Codex");
    public string Name => "Codex";

    public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userPrompt, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ProviderHttp.EnsureConfigured(_settings, Name);
        using var request = new HttpRequestMessage(HttpMethod.Post, ProviderHttp.Endpoint(_settings, "responses"));
        ProviderHttp.AddBearer(request, _settings, Name);
        request.Content = JsonContent.Create(new { model = _settings.ChatModel, instructions = systemPrompt, input = userPrompt, stream = true, store = false });
        using var response = await clients.CreateClient("ai-providers").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await ProviderHttp.EnsureSuccessAsync(response, Name, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
            var data = line[5..].TrimStart();
            if (data is "[DONE]" or "") continue;
            using var json = JsonDocument.Parse(data);
            if (json.RootElement.TryGetProperty("type", out var type) && type.GetString() == "response.output_text.delta" &&
                json.RootElement.TryGetProperty("delta", out var delta))
                yield return delta.GetString() ?? "";
        }
    }
}
