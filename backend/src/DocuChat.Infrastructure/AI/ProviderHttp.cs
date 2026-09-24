using System.Net;
using System.Net.Http.Headers;
using DocuChat.Application;

namespace DocuChat.Infrastructure.AI;

internal static class ProviderHttp
{
    /// <summary>
    /// Sends a request, retrying with exponential backoff on transient upstream
    /// failures (429/502/503/504) that AI providers return during demand spikes.
    /// A fresh <see cref="HttpRequestMessage"/> is built per attempt because a
    /// request (and its content) cannot be reused after being sent.
    /// </summary>
    public static async Task<HttpResponseMessage> SendWithRetryAsync(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken,
        int maxAttempts = 3)
    {
        for (var attempt = 1; ; attempt++)
        {
            var request = requestFactory();
            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, completionOption, cancellationToken);
            }
            finally
            {
                request.Dispose();
            }

            if (response.IsSuccessStatusCode || attempt >= maxAttempts || !IsTransient(response.StatusCode))
                return response;

            response.Dispose();
            var delay = TimeSpan.FromMilliseconds(400 * Math.Pow(2, attempt - 1)); // 400ms, 800ms, …
            await Task.Delay(delay, cancellationToken);
        }
    }

    private static bool IsTransient(HttpStatusCode status) =>
        (int)status == 429
        || status is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

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
