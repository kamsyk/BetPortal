using BpDbLayer.Models;
using BpDbLayer.Utils;
using Microsoft.EntityFrameworkCore;

namespace DbMigration.MsSql
{
    public class BpDbContext(DbContextOptions<BpDbContext> options)
        : DbContext(options)
    {
        public DbSet<BpUser> BpUsers { get; set; }

        public DbSet<BpRole> BpRoles { get; set; }

        public DbSet<UserToken> UserTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<BpUser>().HasData(
               new BpUser
               {
                   Id = 1,
                   UserName = "admin",
                   Email = "admin@betportal.com",
                   Password = PasswordHasher.HashPassword("admin"),
                   FirstName = "System",
                   Surname = "Administrator",
                   IsActive = true,
                   CreatedDate = DateTime.Now.ToUniversalTime(),
               });
        }
    }
}
