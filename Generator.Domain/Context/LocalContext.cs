using Generator.Domain.Core.Entities.Local;
using Microsoft.EntityFrameworkCore;

namespace Generator.Domain.Context;

public class LocalContext : DbContext
{
    private static bool _migrated = false;

    /// <summary>Set by the design-time factory so EF tooling can scaffold migrations
    /// without triggering the constructor's Database.Migrate() call.</summary>
    internal static bool SkipMigration = false;

    public LocalContext()
    {
        if (_migrated == false && SkipMigration == false)
        {
            _migrated = true;
            this.Database.Migrate();
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "LocalProjectDatabase.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ConversationMessage> ConversationMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Conversation>(c =>
        {
            c.HasKey(x => x.Id);
            c.HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConversationMessage>(m =>
        {
            m.HasKey(x => x.Id);
            m.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
