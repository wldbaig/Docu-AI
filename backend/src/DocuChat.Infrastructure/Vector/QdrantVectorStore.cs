using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DocuChat.Application;
using Microsoft.Extensions.Options;

namespace DocuChat.Infrastructure.Vector;

public sealed class QdrantVectorStore(IHttpClientFactory clients, IOptions<VectorStoreOptions> options) : IVectorStore
{
    private readonly VectorStoreOptions _options = options.Value;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private int? _vectorSize;

    public async Task UpsertAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken)
    {
        if (records.Count == 0) return;
        var vectorSize = records[0].Vector.Length;
        if (vectorSize == 0 || records.Any(x => x.Vector.Length != vectorSize))
            throw new ValidationException("Embedding vectors must be non-empty and have a consistent size.");
        await EnsureCollectionAsync(vectorSize, cancellationToken);
        var points = records.Select(x => new
        {
            id = x.ChunkId,
            vector = x.Vector,
            payload = new { userId = x.UserId.ToString(), documentId = x.DocumentId.ToString(), fileName = x.FileName, chunkIndex = x.ChunkIndex, content = x.Content }
        });
        using var response = await SendAsync(HttpMethod.Put, $"collections/{Collection}/points?wait=true", new { points }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(Guid userId, float[] queryVector, int topK, IReadOnlyList<Guid>? documentIds, CancellationToken cancellationToken)
    {
        await EnsureCollectionAsync(queryVector.Length, cancellationToken);
        var must = new List<object> { new { key = "userId", match = new { value = userId.ToString() } } };
        if (documentIds is { Count: > 0 })
            must.Add(new { key = "documentId", match = new { any = documentIds.Select(x => x.ToString()).ToArray() } });
        using var response = await SendAsync(HttpMethod.Post, $"collections/{Collection}/points/query", new
        {
            query = queryVector, filter = new { must }, limit = Math.Clamp(topK, 1, 10), with_payload = true
        }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var result = json.RootElement.GetProperty("result");
        var points = result.ValueKind == JsonValueKind.Array ? result : result.GetProperty("points");
        return points.EnumerateArray().Select(point =>
        {
            var payload = point.GetProperty("payload");
            return new VectorSearchResult(
                Guid.Parse(point.GetProperty("id").GetString()!),
                Guid.Parse(payload.GetProperty("documentId").GetString()!),
                payload.GetProperty("fileName").GetString() ?? "Unknown",
                payload.GetProperty("chunkIndex").GetInt32(),
                payload.GetProperty("content").GetString() ?? "",
                point.GetProperty("score").GetDouble());
        }).ToArray();
    }

    public async Task DeleteDocumentAsync(Guid userId, Guid documentId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Post, $"collections/{Collection}/points/delete?wait=true", new
        {
            filter = new
            {
                must = new object[]
                {
                    new { key = "userId", match = new { value = userId.ToString() } },
                    new { key = "documentId", match = new { value = documentId.ToString() } }
                }
            }
        }, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return;
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task EnsureCollectionAsync(int vectorSize, CancellationToken cancellationToken)
    {
        if (_vectorSize == vectorSize) return;
        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_vectorSize == vectorSize) return;
            using var get = await SendAsync(HttpMethod.Get, $"collections/{Collection}", null, cancellationToken);
            if (get.StatusCode == HttpStatusCode.NotFound)
            {
                using var create = await SendAsync(HttpMethod.Put, $"collections/{Collection}", new { vectors = new { size = vectorSize, distance = "Cosine" } }, cancellationToken);
                await EnsureSuccessAsync(create, cancellationToken);
            }
            else
            {
                await EnsureSuccessAsync(get, cancellationToken);
                using var json = JsonDocument.Parse(await get.Content.ReadAsStreamAsync(cancellationToken));
                var configuredSize = json.RootElement.GetProperty("result").GetProperty("config").GetProperty("params").GetProperty("vectors").GetProperty("size").GetInt32();
                if (configuredSize != vectorSize)
                    throw new ExternalServiceException($"Qdrant collection '{_options.CollectionName}' expects {configuredSize}-dimension vectors, but the selected embedding model returned {vectorSize}. Use a new collection name or reindex all documents.");
            }
            await EnsurePayloadIndexAsync("userId", cancellationToken);
            await EnsurePayloadIndexAsync("documentId", cancellationToken);
            _vectorSize = vectorSize;
        }
        finally { _initializationLock.Release(); }
    }

    private async Task EnsurePayloadIndexAsync(string fieldName, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Put, $"collections/{Collection}/index?wait=true", new { field_name = fieldName, field_schema = "keyword" }, cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Conflict)
            await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        if (!string.Equals(_options.Provider, "Qdrant", StringComparison.OrdinalIgnoreCase))
            throw new ExternalServiceException($"Unsupported vector store '{_options.Provider}'. Available: Qdrant.");
        var baseUrl = _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + '/';
        using var request = new HttpRequestMessage(method, new Uri(new Uri(baseUrl), path));
        if (!string.IsNullOrWhiteSpace(_options.ApiKey)) request.Headers.Add("api-key", _options.ApiKey);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await clients.CreateClient("qdrant").SendAsync(request, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new ExternalServiceException($"Qdrant request failed ({(int)response.StatusCode}): {body[..Math.Min(body.Length, 500)]}");
    }

    private string Collection => Uri.EscapeDataString(_options.CollectionName);
}
