using DocuChat.Domain;

namespace DocuChat.Application;

public sealed class AuthService(IUserRepository users, IPasswordService passwords, IAccessTokenService tokens, IUnitOfWork unitOfWork) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.ExistsByEmailAsync(email, cancellationToken))
            throw new ConflictException("An account with this email already exists.");

        var user = new User { Email = email, PasswordHash = "pending" };
        user.PasswordHash = passwords.Hash(user, request.Password);
        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new AuthResponse(tokens.Create(user), user.Email);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.FindByEmailAsync(email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");
        if (!passwords.Verify(user, user.PasswordHash, request.Password))
            throw new UnauthorizedAccessException("Invalid email or password.");
        return new AuthResponse(tokens.Create(user), user.Email);
    }
}
