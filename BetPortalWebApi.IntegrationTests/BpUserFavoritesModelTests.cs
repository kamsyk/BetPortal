using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

public sealed class BpUserFavoritesModelTests
{
    [Theory]
    [InlineData("runtime")]
    [InlineData("sqlserver")]
    [InlineData("mysql")]
    public void Favorites_configures_single_target_uniqueness_and_all_relationships(string provider)
    {
        using DbContext context = provider switch
        {
            "sqlserver" => new DbMigration.MsSql.BpDbContext(
                new DbContextOptionsBuilder<DbMigration.MsSql.BpDbContext>()
                    .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=FavoritesModelOnly;Integrated Security=true")
                    .Options),
            "mysql" => new DbMigration.MySql.BpDbContext(
                new DbContextOptionsBuilder<DbMigration.MySql.BpDbContext>()
                    .UseMySql("Server=localhost;Database=FavoritesModelOnly;User=model-only",
                        new MySqlServerVersion(new Version(8, 0, 21)))
                    .Options),
            _ => new BpDbLayer.BpDbContext(
                new DbContextOptionsBuilder<BpDbLayer.BpDbContext>()
                    .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=FavoritesModelOnly;Integrated Security=true")
                    .Options),
        };

        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(BpUserFavorites));
        Assert.NotNull(entity);
        Assert.Equal("BpUserFavorites", entity.GetTableName());
        var primaryKey = Assert.Single(entity.FindPrimaryKey()!.Properties);
        Assert.Equal(nameof(BpUserFavorites.Id), primaryKey.Name);
        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd, primaryKey.ValueGenerated);
        Assert.Equal(10, entity.GetProperties().Count());
        Assert.DoesNotContain(entity.GetProperties(), property => property.IsShadowProperty());

        var targets = new[]
        {
            (nameof(BpUserFavorites.CountryId), typeof(Country), nameof(BpUserFavorites.Country)),
            (nameof(BpUserFavorites.SportId), typeof(Sport), nameof(BpUserFavorites.Sport)),
            (nameof(BpUserFavorites.CompetitionId), typeof(Competition), nameof(BpUserFavorites.Competition)),
            (nameof(BpUserFavorites.CompetitorId), typeof(Competitor), nameof(BpUserFavorites.Competitor)),
        };
        Assert.Equal(7, entity.GetForeignKeys().Count());
        var check = Assert.Single(entity.GetCheckConstraints());
        Assert.Equal("CK_BpUserFavorites_ExactlyOneTarget", check.Name);
        Assert.EndsWith(") = 1", check.Sql);
        Assert.Equal(4, entity.GetIndexes().Count(index => index.IsUnique));
        foreach (var (propertyName, targetType, navigationName) in targets)
        {
            Assert.True(entity.FindProperty(propertyName)!.IsNullable);
            var foreignKey = Assert.Single(entity.GetForeignKeys(), key => key.Properties.Single().Name == propertyName);
            Assert.Equal(targetType, foreignKey.PrincipalEntityType.ClrType);
            Assert.Equal(navigationName, foreignKey.DependentToPrincipal?.Name);
            Assert.False(foreignKey.IsRequired);
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
            var index = Assert.Single(entity.GetIndexes(), index =>
                index.IsUnique && index.Properties.Select(property => property.Name)
                    .SequenceEqual(new[] { nameof(BpUserFavorites.UserId), propertyName }));
            if (provider == "mysql")
            {
                Assert.Null(index.GetFilter());
            }
            else
            {
                Assert.Equal($"[{propertyName}] IS NOT NULL", index.GetFilter());
            }
            Assert.Contains($"CASE WHEN {propertyName} IS NOT NULL THEN 1 ELSE 0 END", check.Sql);
        }

        foreach (var (propertyName, navigationName) in new[]
        {
            (nameof(BpUserFavorites.UserId), nameof(BpUserFavorites.User)),
            (nameof(BpUserFavorites.CreatedByUserId), nameof(BpUserFavorites.CreatedByUser)),
            (nameof(BpUserFavorites.ModifiedByUserId), nameof(BpUserFavorites.ModifiedByUser)),
        })
        {
            var foreignKey = Assert.Single(entity.GetForeignKeys(), key => key.Properties.Single().Name == propertyName);
            Assert.Equal(typeof(BpUser), foreignKey.PrincipalEntityType.ClrType);
            Assert.Equal(navigationName, foreignKey.DependentToPrincipal?.Name);
            Assert.True(foreignKey.IsRequired);
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        }

        var sql = context.Database.GenerateCreateScript();
        Assert.Contains("CK_BpUserFavorites_ExactlyOneTarget", sql);
        foreach (var (propertyName, _, _) in targets)
        {
            if (provider != "mysql")
            {
                Assert.Contains($"WHERE [{propertyName}] IS NOT NULL", sql);
            }
        }
    }
}
