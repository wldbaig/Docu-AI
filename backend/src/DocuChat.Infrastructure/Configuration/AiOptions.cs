namespace DocuChat.Infrastructure;

public sealed class AiOptions
{
    public const string SectionName = "AI";
    public string ChatProvider { get; init; } = "OpenAI";
    public string EmbeddingProvider { get; init; } = "OpenAI";
    public Dictionary<string, ModelProviderOptions> Providers { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public ModelProviderOptions Provider(string name) => Providers.TryGetValue(name, out var value)
        ? value
        : throw new InvalidOperationException($"AI provider '{name}' is not configured under AI:Providers.");
}
