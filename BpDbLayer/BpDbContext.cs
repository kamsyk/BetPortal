using BpDbLayer.Models;
using BpDbLayer.Utils;
using Microsoft.EntityFrameworkCore;

namespace BpDbLayer
{
    public class BpDbContext(DbContextOptions<BpDbContext> options)
        : DbContext(options)
    {
        public DbSet<BpUser> BpUsers { get; set; }

        public DbSet<BpRole> BpRoles { get; set; }

        public DbSet<BpUser_BpRole> BpUsers_BpRoles { get; set; }

        public DbSet<UserToken> UserTokens { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BpDbContext).Assembly);

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
                   CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                   ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
               });

            modelBuilder.Entity<BpRole>().HasData(
               new BpRole
               {
                   Id = 1,
                   RoleName = "Administrator",
                   Description = "System Administrator with full access",
                   CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                   ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
               });
        }
    }
}