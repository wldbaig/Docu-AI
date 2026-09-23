using DocuChat.Application;
using DocuChat.Domain;
using Microsoft.EntityFrameworkCore;

namespace DocuChat.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Chunk> Chunks => Set<Chunk>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Ids are assigned by the application (Guid.NewGuid()), not the database.
            // Without this, SQL Server treats a preset key as an existing row and
            // issues UPDATE instead of INSERT for new entities added to a tracked graph.
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(254);
        });
        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.FileName).HasMaxLength(260);
            entity.Property(x => x.Status).HasConversion<string>();
            entity.HasOne(x => x.User).WithMany(x => x.Documents).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Chunk>(entity =>
        {
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasIndex(x => new { x.DocumentId, x.Index }).IsUnique();
            entity.HasOne(x => x.Document).WithMany(x => x.Chunks).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Role).HasConversion<string>();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);
}
