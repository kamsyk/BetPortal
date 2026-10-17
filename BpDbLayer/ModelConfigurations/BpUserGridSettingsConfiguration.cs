using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.ModelConfigurations
{
    public class UserGridSettingsConfiguration : IEntityTypeConfiguration<UserGridSettings>
    {
        public void Configure(EntityTypeBuilder<UserGridSettings> builder)
        {
            builder.HasOne(ugs => ugs.User)
                .WithMany()
                .HasForeignKey(ugs => ugs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(ugs => new { ugs.UserId, ugs.PageIdentifier })
                .IsUnique();
        }
    }
}
