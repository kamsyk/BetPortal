using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DbMigration.MySql
{
    public class BpDbContext(DbContextOptions<BpDbContext> options)
        : DbContext(options)
    {
        public DbSet<BpUser> BpUsers { get; set; }

        public DbSet<BpRole> BpRoles { get; set; }

        public DbSet<UserToken> UserTokens { get; set; }

        public DbSet<Competitor> Competitors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new BpDbLayer.ModelConfigurations.CompetitorConfiguration());
        }
    }
}
