using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.ModelConfigurations
{
    public class CountryConfiguration : IEntityTypeConfiguration<Country>
    {
        public void Configure(EntityTypeBuilder<Country> builder)
        {
            builder.HasIndex(c => c.Name)
                .IsUnique();

            builder.HasOne(c => c.ModifiedByUser)
                .WithMany()
                .HasForeignKey(c => c.ModifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
