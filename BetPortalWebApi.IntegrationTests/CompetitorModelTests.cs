using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

public sealed class CompetitorModelTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Competitor_has_required_properties_and_restricted_audit_relationships(bool migrationContext)
    {
        using DbContext context = migrationContext
            ? new DbMigration.MsSql.BpDbContext(
                new DbContextOptionsBuilder<DbMigration.MsSql.BpDbContext>()
                    .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=CompetitorModelOnly;Integrated Security=true")
                    .Options)
            : new BpDbLayer.BpDbContext(
                new DbContextOptionsBuilder<BpDbLayer.BpDbContext>()
                    .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=CompetitorModelOnly;Integrated Security=true")
                    .Options);

        var entity = context.Model.FindEntityType(typeof(Competitor));
        Assert.NotNull(entity);
        Assert.Equal("Competitors", entity.GetTableName());
        Assert.Equal(nameof(Competitor.Id), Assert.Single(entity.FindPrimaryKey()!.Properties).Name);
        Assert.Equal(7, entity.GetProperties().Count());
        Assert.All(entity.GetProperties(), property =>
        {
            Assert.False(property.IsShadowProperty());
            Assert.False(property.IsNullable);
        });
        Assert.Equal(100, entity.FindProperty(nameof(Competitor.Name))!.GetMaxLength());
        Assert.Equal(2048, entity.FindProperty(nameof(Competitor.WebSiteUrl))!.GetMaxLength());
        Assert.DoesNotContain(entity.GetIndexes(), index =>
            index.IsUnique && index.Properties.Any(property => property.Name == nameof(Competitor.Name)));

        var foreignKeys = entity.GetForeignKeys().ToList();
        Assert.Equal(2, foreignKeys.Count);
        foreach (var (propertyName, navigationName) in new[]
        {
            (nameof(Competitor.CreatedByUserId), nameof(Competitor.CreatedByUser)),
            (nameof(Competitor.ModifiedByUserId), nameof(Competitor.ModifiedByUser)),
        })
        {
            var foreignKey = Assert.Single(foreignKeys, key => key.Properties.Single().Name == propertyName);
            Assert.Equal(typeof(BpUser), foreignKey.PrincipalEntityType.ClrType);
            Assert.Equal(nameof(BpUser.Id), Assert.Single(foreignKey.PrincipalKey.Properties).Name);
            Assert.Equal(navigationName, foreignKey.DependentToPrincipal?.Name);
            Assert.True(foreignKey.IsRequired);
            Assert.False(foreignKey.IsUnique);
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        }
    }
}
