using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DocuChat.Application;
using Microsoft.Extensions.Options;

namespace DocuChat.Infrastructure.AI;

public sealed class GeminiChatProvider(IHttpClientFactory clients, IOptions<AiOptions> options) : IChatModelProvider
{
    private readonly ModelProviderOptions _settings = options.Value.Provider("Gemini");
    public string Name => "Gemini";

    public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userPrompt, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ProviderHttp.EnsureConfigured(_settings, Name);
        var model = _settings.ChatModel.StartsWith("models/", StringComparison.Ordinal) ? _settings.ChatModel : $"models/{_settings.ChatModel}";
        HttpRequestMessage BuildRequest()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, ProviderHttp.Endpoint(_settings, $"{model}:streamGenerateContent?alt=sse"));
            request.Headers.Add("x-goog-api-key", _settings.ApiKey);
            request.Content = JsonContent.Create(new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
                generationConfig = new { temperature = 0.1 }
            });
            return request;
        }
        using var response = await ProviderHttp.SendWithRetryAsync(clients.CreateClient("ai-providers"), BuildRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await ProviderHttp.EnsureSuccessAsync(response, Name, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
            var data = line[5..].TrimStart();
            if (data.Length == 0) continue;
            using var json = JsonDocument.Parse(data);
            if (!json.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) continue;
            if (!candidates[0].TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts)) continue;
            foreach (var part in parts.EnumerateArray())
                if (part.TryGetProperty("text", out var text)) yield return text.GetString() ?? "";
        }
    }
}
