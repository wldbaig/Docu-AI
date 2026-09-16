using System.Runtime.CompilerServices;
using DocuChat.Application;

namespace DocuChat.Tests;

internal sealed class FakeChatProvider(string name, string token) : IChatModelProvider
{
    public string Name => name;

    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        yield return token;
    }
}
