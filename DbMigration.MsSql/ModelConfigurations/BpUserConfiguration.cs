using BpDbLayer.Models;
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
                    ////the password is "admin" hashed using Argon2id
                    Password = "q4vjB17WZRLQ6SvcWmqcNh2FhijuX8F+7lZo2S9c2tLwATTMEN13M8vJ+0oKeIqE",
                    FirstName = "System",
                    Surname = "Administrator",
                    IsActive = true,
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedByUserId = 1,
                    ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedByUserId = 1,
                });
        }
    }
}
