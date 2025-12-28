#pragma warning disable IDE0320 // Use collection expression for fluent

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
            /*
            modelBuilder.Entity<BpUser_BpRole>(entity =>
            {
                // Composite primary key
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                // Foreign key to User
                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.BpUser_BpRole)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .;

                // Foreign key to Role
                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.BpUser_BpRole)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });*/
        }
    }
}

#pragma warning restore IDE0320