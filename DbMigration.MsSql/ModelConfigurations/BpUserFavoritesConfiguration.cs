using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BpDbLayer.ModelConfigurations
{
    public class BpUserFavoritesConfiguration : IEntityTypeConfiguration<BpUserFavorites>
    {
        public void Configure(EntityTypeBuilder<BpUserFavorites> builder)
        {
            const string exactlyOneTarget = "(CASE WHEN CountryId IS NOT NULL THEN 1 ELSE 0 END"
                + " + CASE WHEN SportId IS NOT NULL THEN 1 ELSE 0 END"
                + " + CASE WHEN CompetitionId IS NOT NULL THEN 1 ELSE 0 END"
                + " + CASE WHEN CompetitorId IS NOT NULL THEN 1 ELSE 0 END) = 1";

            builder.ToTable("BpUserFavorites", table => table.HasCheckConstraint(
                "CK_BpUserFavorites_ExactlyOneTarget", exactlyOneTarget));

            builder.HasIndex(f => new { f.UserId, f.CountryId }).IsUnique();
            builder.HasIndex(f => new { f.UserId, f.SportId }).IsUnique();
            builder.HasIndex(f => new { f.UserId, f.CompetitionId }).IsUnique();
            builder.HasIndex(f => new { f.UserId, f.CompetitorId }).IsUnique();

            builder.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Country)
                .WithMany()
                .HasForeignKey(f => f.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Sport)
                .WithMany()
                .HasForeignKey(f => f.SportId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Competition)
                .WithMany()
                .HasForeignKey(f => f.CompetitionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Competitor)
                .WithMany()
                .HasForeignKey(f => f.CompetitorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.CreatedByUser)
                .WithMany()
                .HasForeignKey(f => f.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.ModifiedByUser)
                .WithMany()
                .HasForeignKey(f => f.ModifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
