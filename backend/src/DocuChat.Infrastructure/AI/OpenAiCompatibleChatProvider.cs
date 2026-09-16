using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DocuChat.Application;
using Microsoft.Extensions.Options;

namespace DocuChat.Infrastructure.AI;

public abstract class OpenAiCompatibleChatProvider(
    IHttpClientFactory clients,
    IOptions<AiOptions> options,
    string providerName) : IChatModelProvider
{
    private readonly ModelProviderOptions _settings = options.Value.Provider(providerName);

    public string Name => providerName;

    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ProviderHttp.EnsureConfigured(_settings, Name);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            ProviderHttp.Endpoint(_settings, "chat/completions"));
        ProviderHttp.AddBearer(request, _settings, Name);
        request.Content = JsonContent.Create(new
        {
            model = _settings.ChatModel,
            stream = true,
            temperature = 0.1,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        });

        using var response = await clients.CreateClient("ai-providers")
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await ProviderHttp.EnsureSuccessAsync(response, Name, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line[5..].TrimStart();
            if (data == "[DONE]")
            {
                yield break;
            }

            using var json = JsonDocument.Parse(data);
            var choices = json.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("content", out var content))
            {
                yield return content.GetString() ?? "";
            }
        }
    }
}
