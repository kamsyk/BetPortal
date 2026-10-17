using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.ModelConfigurations
{
    public class BpUserTokenConfiguration : IEntityTypeConfiguration<BpUserToken>
    {
        public void Configure(EntityTypeBuilder<BpUserToken> builder)
        {
            // One-to-one: BpUser <-> BpUserToken
            builder.HasOne(t => t.User)
                .WithOne(u => u.UserToken)
                .HasForeignKey<BpUserToken>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
