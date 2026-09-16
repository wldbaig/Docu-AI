using Microsoft.Extensions.Options;

namespace DocuChat.Infrastructure.AI;

public sealed class OpenAiChatProvider(IHttpClientFactory clients, IOptions<AiOptions> options)
    : OpenAiCompatibleChatProvider(clients, options, "OpenAI");
