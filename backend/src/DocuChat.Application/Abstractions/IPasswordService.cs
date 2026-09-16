using DocuChat.Domain;

namespace DocuChat.Application;

public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string passwordHash, string password);
}
