using Microsoft.EntityFrameworkCore.Design;

namespace Generator.Domain.Context;

/// <summary>
/// Used by "dotnet ef" tooling only. Prevents the LocalContext constructor from
/// running Database.Migrate() while a new migration is being scaffolded.
/// </summary>
public class LocalContextDesignTimeFactory : IDesignTimeDbContextFactory<LocalContext>
{
    public LocalContext CreateDbContext(string[] args)
    {
        LocalContext.SkipMigration = true;
        return new LocalContext();
    }
}
