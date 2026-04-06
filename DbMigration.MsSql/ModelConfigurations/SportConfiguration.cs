using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.ModelConfigurations
{
    public class SportConfiguration : IEntityTypeConfiguration<Sport>
    {
        public void Configure(EntityTypeBuilder<Sport> builder)
        {
            builder.HasOne(s => s.ModifiedByUser)
                .WithMany()
                .HasForeignKey(s => s.ModifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
