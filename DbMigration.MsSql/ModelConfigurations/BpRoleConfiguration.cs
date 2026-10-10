using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.ModelConfigurations
{
    public class BpRoleConfiguration : IEntityTypeConfiguration<BpRole>
    {
        public const int AdminRoleId = 1;

        public const int BasicUserRoleId = 2;

        public const int SubscribedUserL1RoleId = 3;

        public void Configure(EntityTypeBuilder<BpRole> builder)
        {
            builder.HasData(
                new BpRole
                {
                    Id = AdminRoleId,
                    RoleName = "Admin",
                    Description = "Administrator role with full permissions",
                    IsActive = true,
                    CreatedByUserId = BpUserConfiguration.AdminUserId,
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedByUserId = BpUserConfiguration.AdminUserId,
                    ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new BpRole
                {
                    Id = BasicUserRoleId,
                    RoleName = "BasicUser",
                    Description = "Basic user role",
                    IsActive = true,
                    CreatedByUserId = 1,
                    CreatedDate = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedByUserId = 1,
                    ModifiedDate = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                },
                new BpRole
                {
                    Id = SubscribedUserL1RoleId,
                    RoleName = "SubscribedUserL1",
                    Description = "Level 1 subscribed user role",
                    IsActive = true,
                    CreatedByUserId = 1,
                    CreatedDate = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedByUserId = 1,
                    ModifiedDate = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                });
        }
    }
}
