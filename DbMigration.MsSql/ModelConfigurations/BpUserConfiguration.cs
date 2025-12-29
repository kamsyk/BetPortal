using BpDbLayer.Models;
using BpDbLayer.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.Configurations
{
    public class BpUserConfiguration : IEntityTypeConfiguration<BpUser>
    {
        public void Configure(EntityTypeBuilder<BpUser> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.UserName).IsRequired().HasMaxLength(100);
            builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Surname).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Email).HasMaxLength(255);

            builder.HasData(
                new BpUser
                {
                    Id = 1,
                    UserName = "admin",
                    Email = "admin@betportal.com",
                    ////Password = PasswordHasher.HashPassword("admin"),
                    FirstName = "System",
                    Surname = "Administrator",
                    IsActive = true,
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                });
        }
    }
}
