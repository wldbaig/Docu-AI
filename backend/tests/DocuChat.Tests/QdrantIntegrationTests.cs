using DocuChat.Application;
using DocuChat.Infrastructure;
using DocuChat.Infrastructure.Vector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DocuChat.Tests;

public sealed class QdrantIntegrationTests
{
    [Fact]
    public async Task UpsertSearchAndDelete_RespectsTenantFilter()
    {
        if (Environment.GetEnvironmentVariable("RUN_QDRANT_TESTS") != "1") return;
        var collection = $"docuchat_test_{Guid.NewGuid():N}";
        var services = new ServiceCollection().AddHttpClient("qdrant").Services.BuildServiceProvider();
        var options = Options.Create(new VectorStoreOptions { BaseUrl = "http://localhost:6333", CollectionName = collection });
        var store = new QdrantVectorStore(services.GetRequiredService<IHttpClientFactory>(), options);
        var user = Guid.NewGuid(); var otherUser = Guid.NewGuid(); var document = Guid.NewGuid();
        try
        {
            await store.UpsertAsync([
                new(Guid.NewGuid(), user, document, "mine.txt", 0, "relevant", [1, 0, 0]),
                new(Guid.NewGuid(), user, document, "mine.txt", 1, "less relevant", [0.5f, 0.5f, 0]),
                new(Guid.NewGuid(), otherUser, Guid.NewGuid(), "other.txt", 0, "must stay isolated", [1, 0, 0])
            ], CancellationToken.None);

            var results = await store.SearchAsync(user, [1, 0, 0], 5, null, CancellationToken.None);
            Assert.Equal(2, results.Count);
            Assert.All(results, result => Assert.Equal(document, result.DocumentId));
            Assert.Equal("relevant", results[0].Content);

            await store.DeleteDocumentAsync(user, document, CancellationToken.None);
            Assert.Empty(await store.SearchAsync(user, [1, 0, 0], 5, null, CancellationToken.None));
        }
        finally
        {
            using var client = new HttpClient();
            await client.DeleteAsync($"http://localhost:6333/collections/{collection}");
        }
    }
}
