namespace DocuChat.Application;

public interface IChatModelProvider
{
    string Name { get; }
    IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
}
