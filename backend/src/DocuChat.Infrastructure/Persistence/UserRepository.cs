using DocuChat.Application;
using DocuChat.Domain;
using Microsoft.EntityFrameworkCore;

namespace DocuChat.Infrastructure.Persistence;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(x => x.Email == email, cancellationToken);

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

    public void Add(User user) => db.Users.Add(user);
}
