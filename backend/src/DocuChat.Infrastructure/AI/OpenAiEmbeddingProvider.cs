using System.Net.Http.Json;
using System.Text.Json;
using DocuChat.Application;
using Microsoft.Extensions.Options;

namespace DocuChat.Infrastructure.AI;

public sealed class OpenAiEmbeddingProvider(IHttpClientFactory clients, IOptions<AiOptions> options) : IEmbeddingModelProvider
{
    private readonly ModelProviderOptions _settings = options.Value.Provider("OpenAI");
    public string Name => "OpenAI";

    public async Task<IReadOnlyList<float[]>> CreateAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken)
    {
        ProviderHttp.EnsureConfigured(_settings, Name, embedding: true);
        using var request = new HttpRequestMessage(HttpMethod.Post, ProviderHttp.Endpoint(_settings, "embeddings"));
        ProviderHttp.AddBearer(request, _settings, Name);
        request.Content = JsonContent.Create(new { model = _settings.EmbeddingModel, input = inputs });
        using var response = await clients.CreateClient("ai-providers").SendAsync(request, cancellationToken);
        await ProviderHttp.EnsureSuccessAsync(response, Name, cancellationToken);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        return json.RootElement.GetProperty("data").EnumerateArray().OrderBy(x => x.GetProperty("index").GetInt32())
            .Select(x => x.GetProperty("embedding").EnumerateArray().Select(v => v.GetSingle()).ToArray()).ToArray();
    }
}
