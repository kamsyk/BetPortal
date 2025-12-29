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
        }
    }
}
