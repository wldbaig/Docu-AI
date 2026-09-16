using Microsoft.Extensions.Options;

namespace DocuChat.Infrastructure.AI;

public sealed class GrokChatProvider(IHttpClientFactory clients, IOptions<AiOptions> options)
    : OpenAiCompatibleChatProvider(clients, options, "Grok");
