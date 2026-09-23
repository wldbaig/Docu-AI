namespace DocuChat.Application;

public sealed record ProviderCatalogDto(
    IReadOnlyList<ProviderInfo> Chat,
    IReadOnlyList<ProviderInfo> Embedding,
    string ActiveChat,
    string ActiveEmbedding);
