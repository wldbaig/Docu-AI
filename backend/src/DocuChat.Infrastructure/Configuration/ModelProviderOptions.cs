namespace DocuChat.Infrastructure;

public sealed class ModelProviderOptions
{
    public string ApiKey { get; init; } = "";
    public string BaseUrl { get; init; } = "";
    public string ChatModel { get; init; } = "";
    public string EmbeddingModel { get; init; } = "";
}
