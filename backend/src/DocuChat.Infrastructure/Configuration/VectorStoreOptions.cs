namespace DocuChat.Infrastructure;

public sealed class VectorStoreOptions
{
    public const string SectionName = "VectorStore";
    public string Provider { get; init; } = "Qdrant";
    public string BaseUrl { get; init; } = "http://localhost:6333";
    public string ApiKey { get; init; } = "";
    public string CollectionName { get; init; } = "docuchat_chunks";
}
