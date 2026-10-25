using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.ModelConfigurations;

public class BpUserSessionConfiguration : IEntityTypeConfiguration<BpUserSession>
{
    public void Configure(EntityTypeBuilder<BpUserSession> builder)
    {
        builder.HasIndex(session => session.TokenHash).IsUnique();

        builder.Property(session => session.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(session => session.IpAddress).HasMaxLength(45).IsRequired();
        builder.Property(session => session.UserAgent).HasMaxLength(512).IsRequired();

        builder.HasOne(session => session.User)
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
