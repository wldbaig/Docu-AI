using System.Net.Http.Json;
using System.Text.Json;
using DocuChat.Application;
using Microsoft.Extensions.Options;

namespace DocuChat.Infrastructure.AI;

public sealed class GeminiEmbeddingProvider(IHttpClientFactory clients, IOptions<AiOptions> options) : IEmbeddingModelProvider
{
    private readonly ModelProviderOptions _settings = options.Value.Provider("Gemini");
    public string Name => "Gemini";

    public async Task<IReadOnlyList<float[]>> CreateAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken)
    {
        ProviderHttp.EnsureConfigured(_settings, Name, embedding: true);
        var model = _settings.EmbeddingModel.StartsWith("models/", StringComparison.Ordinal) ? _settings.EmbeddingModel : $"models/{_settings.EmbeddingModel}";
        using var request = new HttpRequestMessage(HttpMethod.Post, ProviderHttp.Endpoint(_settings, $"{model}:batchEmbedContents"));
        request.Headers.Add("x-goog-api-key", _settings.ApiKey);
        request.Content = JsonContent.Create(new { requests = inputs.Select(input => new { model, content = new { parts = new[] { new { text = input } } } }) });
        using var response = await clients.CreateClient("ai-providers").SendAsync(request, cancellationToken);
        await ProviderHttp.EnsureSuccessAsync(response, Name, cancellationToken);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        return json.RootElement.GetProperty("embeddings").EnumerateArray()
            .Select(x => x.GetProperty("values").EnumerateArray().Select(v => v.GetSingle()).ToArray()).ToArray();
    }
}
