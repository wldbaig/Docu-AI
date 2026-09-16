using DocuChat.Application;
using DocuChat.Domain;

namespace DocuChat.Infrastructure.Persistence;

public sealed class ChatMessageRepository(AppDbContext db) : IChatMessageRepository
{
    public void Add(ChatMessage message) => db.ChatMessages.Add(message);
}
