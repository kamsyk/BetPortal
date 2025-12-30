using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.ModelConfigurations.DataSeed
{
    public class UserDataSeed : IEntityTypeConfiguration<BpUser>
    {
        private const int AdminUserId = 1;
        private const int AdminRoleId = 1;

        public void Configure(EntityTypeBuilder<BpUser> builder)
        {
            builder.HasData(
                new BpUser
                {
                    Id = AdminUserId,
                    UserName = "admin",
                    Email = "admin@betportal.com",
                    ////the password is "admin" hashed using Argon2id
                    Password = "q4vjB17WZRLQ6SvcWmqcNh2FhijuX8F+7lZo2S9c2tLwATTMEN13M8vJ+0oKeIqE",
                    FirstName = "System",
                    Surname = "Administrator",
                    IsActive = true,
                    CreatedByUserId = AdminUserId,
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedByUserId = AdminUserId,
                    ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                });

            builder.HasData(
                new BpRole
                {
                    Id = AdminRoleId,
                    RoleName = "Admin",
                    Description = "Administrator role with full permissions",
                    IsActive = true,
                    CreatedByUserId = AdminUserId,
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedByUserId = AdminUserId,
                    ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                });

            builder.HasData(
                new BpUserBpRole
                {
                    UserId = AdminUserId,
                    RoleId = AdminRoleId,
                });
        }
    }
}
