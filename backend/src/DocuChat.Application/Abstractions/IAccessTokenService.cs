using DocuChat.Domain;

namespace DocuChat.Application;

public interface IAccessTokenService
{
    string Create(User user);
}
