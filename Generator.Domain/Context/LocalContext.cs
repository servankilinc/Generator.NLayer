using Generator.Domain.Core.Entities.Local;
using Microsoft.EntityFrameworkCore;

namespace Generator.Domain.Context;

public class LocalContext : DbContext
{
    private static bool _migrated = false;
    public LocalContext()
    {
        if (_migrated == false)
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
