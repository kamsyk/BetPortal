using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.ModelConfigurations
{
    public class BpUserLoginProviderConfiguration : IEntityTypeConfiguration<BpUserLoginProvider>
    {
        public void Configure(EntityTypeBuilder<BpUserLoginProvider> builder)
        {
            builder.HasKey(userLoginProvider => new
            {
                userLoginProvider.UserId,
                userLoginProvider.ProviderId,
            });

            builder.HasOne(userLoginProvider => userLoginProvider.User)
                .WithMany(user => user.UserLoginProviders)
                .HasForeignKey(userLoginProvider => userLoginProvider.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(userLoginProvider => userLoginProvider.LoginProvider)
                .WithMany(loginProvider => loginProvider.UserLoginProviders)
                .HasForeignKey(userLoginProvider => userLoginProvider.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
