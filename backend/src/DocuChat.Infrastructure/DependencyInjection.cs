using DocuChat.Application;
using DocuChat.Infrastructure.AI;
using DocuChat.Infrastructure.Persistence;
using DocuChat.Infrastructure.Security;
using DocuChat.Infrastructure.Vector;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocuChat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=docuchat.db"));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<VectorStoreOptions>(configuration.GetSection(VectorStoreOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddHttpClient("ai-providers", client => client.Timeout = TimeSpan.FromMinutes(5));
        services.AddHttpClient("qdrant", client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddSingleton<IChatModelProvider, OpenAiChatProvider>();
        services.AddSingleton<IChatModelProvider, CodexChatProvider>();
        services.AddSingleton<IChatModelProvider, GrokChatProvider>();
        services.AddSingleton<IChatModelProvider, ClaudeChatProvider>();
        services.AddSingleton<IChatModelProvider, GeminiChatProvider>();
        services.AddSingleton<IChatModelProvider, GlmChatProvider>();
        services.AddSingleton<IEmbeddingModelProvider, OpenAiEmbeddingProvider>();
        services.AddSingleton<IEmbeddingModelProvider, GeminiEmbeddingProvider>();
        services.AddSingleton<IEmbeddingService, ConfigurableEmbeddingService>();
        services.AddSingleton<IChatCompletionService, ConfigurableChatCompletionService>();
        services.AddSingleton<IVectorStore, QdrantVectorStore>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IRagService, RagService>();
        services.AddSingleton<ITextExtractor, DocumentTextExtractor>();
        services.AddSingleton<ITextChunker>(_ => new TextChunker());
        return services;
    }
}
