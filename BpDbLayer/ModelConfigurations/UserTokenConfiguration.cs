using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.ModelConfigurations
{
    public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
    {
        public void Configure(EntityTypeBuilder<UserToken> builder)
        {
            // One-to-one: BpUser <-> UserToken
            builder.HasOne(t => t.User)
                .WithOne(u => u.UserToken)
                .HasForeignKey<UserToken>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
