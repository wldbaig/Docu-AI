namespace DocuChat.Application;

public interface IChatCompletionService
{
    IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
}
