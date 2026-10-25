using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BpDbLayer
{
    public class BpDbContext(DbContextOptions<BpDbContext> options)
        : DbContext(options)
    {
        public DbSet<BpUser> BpUsers { get; set; }

        public DbSet<BpRole> BpRoles { get; set; }

        public DbSet<BpUserBpRole> BpUsers_BpRoles { get; set; }

        public DbSet<BpUserToken> BpUserTokens { get; set; }

        public DbSet<Sport> Sports { get; set; }

        public DbSet<Country> Countries { get; set; }

        public DbSet<Competitor> Competitors { get; set; }

        public DbSet<Competition> Competitions { get; set; }

        public DbSet<BpUserFavorites> BpUserFavorites { get; set; }

        public DbSet<BpUserGridSettings> BpUserGridSettings { get; set; }

        public DbSet<BpUserLoginProvider> BpUserLoginProviders { get; set; }

        public DbSet<BpUserSession> BpUserSessions { get; set; }

        public DbSet<LoginProvider> LoginProviders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BpDbContext).Assembly);
        }
    }
}