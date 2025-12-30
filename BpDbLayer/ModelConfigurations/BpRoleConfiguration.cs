using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.ModelConfigurations
{
    public class BpRoleConfiguration : IEntityTypeConfiguration<BpRole>
    {
        public const int AdminRoleId = 1;

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
                });
        }
    }
}
