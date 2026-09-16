using Microsoft.Extensions.Options;

namespace DocuChat.Infrastructure.AI;

public sealed class GlmChatProvider(IHttpClientFactory clients, IOptions<AiOptions> options)
    : OpenAiCompatibleChatProvider(clients, options, "GLM");
