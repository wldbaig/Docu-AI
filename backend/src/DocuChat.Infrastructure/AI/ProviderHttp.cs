using System.Net.Http.Headers;
using DocuChat.Application;

namespace DocuChat.Infrastructure.AI;

internal static class ProviderHttp
{
    public static Uri Endpoint(ModelProviderOptions settings, string relativePath)
    {
        if (!Uri.TryCreate(settings.BaseUrl.EndsWith('/') ? settings.BaseUrl : settings.BaseUrl + '/', UriKind.Absolute, out var baseUri))
            throw new ExternalServiceException("The configured AI provider BaseUrl is invalid.");
        return new Uri(baseUri, relativePath);
    }

    public static void AddBearer(HttpRequestMessage request, ModelProviderOptions settings, string provider)
    {
        EnsureConfigured(settings, provider);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
    }

    public static void EnsureConfigured(ModelProviderOptions settings, string provider, bool embedding = false)
    {
        var model = embedding ? settings.EmbeddingModel : settings.ChatModel;
        if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.BaseUrl) || string.IsNullOrWhiteSpace(model))
            throw new ExternalServiceException($"{provider} is not configured. Set its API key, base URL, and {(embedding ? "embedding" : "chat")} model under AI:Providers:{provider}.");
    }

    public static async Task EnsureSuccessAsync(HttpResponseMessage response, string provider, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new ExternalServiceException($"{provider} request failed ({(int)response.StatusCode}): {body[..Math.Min(body.Length, 500)]}");
    }
}
