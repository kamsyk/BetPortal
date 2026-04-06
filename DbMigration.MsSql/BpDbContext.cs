using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DbMigration.MsSql
{
    public class BpDbContext(DbContextOptions<BpDbContext> options)
        : DbContext(options)
    {
        public DbSet<BpUser> BpUsers { get; set; }

        public DbSet<BpRole> BpRoles { get; set; }

        public DbSet<BpUserBpRole> BpUsers_BpRoles { get; set; }

        public DbSet<UserToken> UserTokens { get; set; }

        public DbSet<Sport> Sports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BpDbContext).Assembly);
        }
    }
}
