using GeneratorWPF.Models.LocalModels;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace GeneratorWPF.Context
{
    public class LocalContext : DbContext
    {
        private static bool _migrated = false;
        public LocalContext()
        {
            if (!_migrated)
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
}
