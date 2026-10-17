using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.ModelConfigurations
{
    public class BpUserGridSettingsConfiguration : IEntityTypeConfiguration<BpUserGridSettings>
    {
        public void Configure(EntityTypeBuilder<BpUserGridSettings> builder)
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
