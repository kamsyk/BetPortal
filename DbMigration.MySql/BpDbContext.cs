using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DbMigration.MySql
{
    public class BpDbContext(DbContextOptions<BpDbContext> options)
        : DbContext(options)
    {
        public DbSet<BpUser> BpUsers { get; set; }

        public DbSet<BpRole> BpRoles { get; set; }

        public DbSet<BpUserToken> BpUserTokens { get; set; }

        public DbSet<Competitor> Competitors { get; set; }

        public DbSet<Sport> Sports { get; set; }

        public DbSet<Country> Countries { get; set; }

        public DbSet<Competition> Competitions { get; set; }

        public DbSet<BpUserFavorites> BpUserFavorites { get; set; }

        public DbSet<BpUserLoginProvider> BpUserLoginProviders { get; set; }

        public DbSet<BpUserSession> BpUserSessions { get; set; }

        public DbSet<LoginProvider> LoginProviders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new BpDbLayer.ModelConfigurations.BpUserConfiguration());

            modelBuilder.ApplyConfiguration(new BpDbLayer.ModelConfigurations.BpRoleConfiguration());

            modelBuilder.ApplyConfiguration(new BpDbLayer.ModelConfigurations.BpUserBpRoleConfiguration());

            modelBuilder.ApplyConfiguration(new BpDbLayer.ModelConfigurations.CompetitorConfiguration());

            modelBuilder.ApplyConfiguration(new BpDbLayer.ModelConfigurations.CompetitionConfiguration());

            modelBuilder.ApplyConfiguration(new BpDbLayer.ModelConfigurations.SportConfiguration());

            modelBuilder.ApplyConfiguration(new BpDbLayer.ModelConfigurations.CountryConfiguration());

            modelBuilder.ApplyConfiguration(new BpDbLayer.ModelConfigurations.BpUserFavoritesConfiguration());

            modelBuilder.ApplyConfiguration(new BpDbLayer.ModelConfigurations.BpUserLoginProviderConfiguration());

            modelBuilder.ApplyConfiguration(new BpDbLayer.ModelConfigurations.BpUserSessionConfiguration());

            modelBuilder.ApplyConfiguration(new BpDbLayer.ModelConfigurations.LoginProviderConfiguration());
        }
    }
}
