using DocuChat.Application;
using DocuChat.Infrastructure;
using DocuChat.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace DocuChat.Tests;

public sealed class ConfigurableAiServicesTests
{
    [Fact]
    public async Task ChatService_UsesConfiguredProvider()
    {
        IChatModelProvider[] providers = [new FakeChatProvider("OpenAI", "wrong"), new FakeChatProvider("Claude", "selected")];
        var service = new ConfigurableChatCompletionService(providers, Options.Create(new AiOptions { ChatProvider = "Claude" }));

        var tokens = new List<string>();
        await foreach (var token in service.StreamAsync("system", "user", null, CancellationToken.None)) tokens.Add(token);

        Assert.Equal(["selected"], tokens);
    }

    [Fact]
    public async Task ChatService_OverridesDefaultWithRequestedProvider()
    {
        IChatModelProvider[] providers = [new FakeChatProvider("OpenAI", "default"), new FakeChatProvider("Claude", "picked")];
        var service = new ConfigurableChatCompletionService(providers, Options.Create(new AiOptions { ChatProvider = "OpenAI" }));

        var tokens = new List<string>();
        await foreach (var token in service.StreamAsync("system", "user", "Claude", CancellationToken.None)) tokens.Add(token);

        Assert.Equal(["picked"], tokens);
    }

    [Fact]
    public async Task ChatService_RejectsUnknownRequestedProvider()
    {
        IChatModelProvider[] providers = [new FakeChatProvider("OpenAI", "default")];
        var service = new ConfigurableChatCompletionService(providers, Options.Create(new AiOptions { ChatProvider = "OpenAI" }));

        await Assert.ThrowsAsync<ValidationException>(async () =>
        {
            await foreach (var _ in service.StreamAsync("system", "user", "Nope", CancellationToken.None)) { }
        });
    }

    [Fact]
    public async Task EmbeddingService_UsesConfiguredProvider()
    {
        IEmbeddingModelProvider[] providers = [new FakeEmbeddingProvider("OpenAI", 1), new FakeEmbeddingProvider("Gemini", 2)];
        var service = new ConfigurableEmbeddingService(providers, Options.Create(new AiOptions { EmbeddingProvider = "Gemini" }));

        var vectors = await service.CreateAsync(["text"], CancellationToken.None);

        Assert.Equal(2, vectors[0][0]);
    }
}
