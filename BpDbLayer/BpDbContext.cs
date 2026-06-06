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

        public DbSet<UserToken> UserTokens { get; set; }

        public DbSet<Sport> Sports { get; set; }

        public DbSet<Country> Countries { get; set; }

        public DbSet<UserGridSettings> UserGridSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BpDbContext).Assembly);
        }
    }
}