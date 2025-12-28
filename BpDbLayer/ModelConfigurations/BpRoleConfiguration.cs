using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.Configurations
{
    public class BpRoleConfiguration : IEntityTypeConfiguration<BpRole>
    {
        public void Configure(EntityTypeBuilder<BpRole> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.RoleName).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Description).IsRequired().HasMaxLength(255);
        }
    }
}
