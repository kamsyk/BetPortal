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

            modelBuilder.Entity<BpRole>().HasData(
               new BpRole
               {
                   Id = 1,
                   RoleName = "Administrator",
                   Description = "System Administrator with full access",
                   CreatedDate = DateTime.Now.ToUniversalTime(),
                   ModifiedDate = DateTime.Now.ToUniversalTime(),
               });

            modelBuilder.Entity<BpUser>()
                .HasMany(u => u.BpRoles)
                .WithMany(r => r.BpUsers)
                .UsingEntity<Dictionary<string, object>>(
                    "BpUserRole",
                    j => j
                        .HasOne<BpRole>()
                        .WithMany()
                        .HasForeignKey("BpRoleId")
                        .HasConstraintName("FK_BpUserRole_BpRoles_BpRoleId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<BpUser>()
                        .WithMany()
                        .HasForeignKey("BpUserId")
                        .HasConstraintName("FK_BpUserRole_BpUsers_BpUserId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("BpUserId", "BpRoleId");
                        j.ToTable("BpUserRoles");
                        j.HasData(
                            new { BpUserId = 1, BpRoleId = 1 } // Assign admin user to Administrator role
                        );
                    });
        }
    }
}
