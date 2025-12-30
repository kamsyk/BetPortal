using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.Configurations
{
    public class BpUser_BpRoleConfiguration : IEntityTypeConfiguration<BpUserBpRole>
    {
        public void Configure(EntityTypeBuilder<BpUserBpRole> builder)
        {
            builder.HasKey(ur => new { ur.UserId, ur.RoleId });

            builder.HasOne(ur => ur.User)
                .WithMany(u => u.BpUser_BpRole)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to Role with custom constraint name
            builder.HasOne(ur => ur.Role)
                .WithMany(r => r.BpUser_BpRole)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
