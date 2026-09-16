using DocuChat.Domain;

namespace DocuChat.Application;

public interface IChatMessageRepository
{
    void Add(ChatMessage message);
}
