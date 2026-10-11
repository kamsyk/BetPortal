using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.ModelConfigurations
{
    public class LoginProviderConfiguration : IEntityTypeConfiguration<LoginProvider>
    {
        public void Configure(EntityTypeBuilder<LoginProvider> builder)
        {
            builder.HasKey(loginProvider => loginProvider.Id);
        }
    }
}
