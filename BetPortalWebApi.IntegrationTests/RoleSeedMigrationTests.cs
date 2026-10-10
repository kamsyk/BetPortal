using BpDbLayer.Models;
using DbMigration.MsSql.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

public sealed class RoleSeedMigrationTests
{
    [Fact]
    public void Migration_inserts_two_active_roles_and_removes_them_on_rollback()
    {
        var migration = new BpMigration_20261010183037();
        var insert = Assert.Single(migration.UpOperations.OfType<InsertDataOperation>(), operation => operation.Table == "BpRoles");
        Assert.Equal(new[] { "Id", "RoleName", "Description", "IsActive", "CreatedDate", "CreatedByUserId", "ModifiedDate", "ModifiedByUserId" }, insert.Columns);
        Assert.Equal(2, insert.Values.GetLength(0));
        Assert.Equal(8, insert.Values.GetLength(1));
        var timestamp = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var names = new[] { "BasicUser", "SubscribedUserL1" };
        for (var row = 0; row < names.Length; row++)
        {
            Assert.Equal(row + 2, insert.Values[row, 0]);
            Assert.Equal(names[row], insert.Values[row, 1]);
            Assert.False(string.IsNullOrWhiteSpace(Assert.IsType<string>(insert.Values[row, 2])));
            Assert.Equal(true, insert.Values[row, 3]);
            Assert.Equal(timestamp, insert.Values[row, 4]);
            Assert.Equal(1, insert.Values[row, 5]);
            Assert.Equal(timestamp, insert.Values[row, 6]);
            Assert.Equal(1, insert.Values[row, 7]);
        }

        var delete = Assert.Single(migration.DownOperations.OfType<DeleteDataOperation>(), operation => operation.Table == "BpRoles");
        Assert.Equal(new[] { "Id" }, delete.KeyColumns);
        Assert.Equal(2, delete.KeyValues.GetLength(0));
        Assert.Equal(2, delete.KeyValues[0, 0]);
        Assert.Equal(3, delete.KeyValues[1, 0]);
    }

    [Fact]
    public void Runtime_migration_snapshot_and_target_model_keep_the_same_role_seeds()
    {
        using var runtime = new BpDbLayer.BpDbContext(
            new DbContextOptionsBuilder<BpDbLayer.BpDbContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=RoleModelOnly;Integrated Security=true")
                .Options);
        using var migrationContext = new DbMigration.MsSql.BpDbContext(
            new DbContextOptionsBuilder<DbMigration.MsSql.BpDbContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=RoleModelOnly;Integrated Security=true")
                .Options);
        var models = new[]
        {
            runtime.GetService<IDesignTimeModel>().Model,
            migrationContext.GetService<IDesignTimeModel>().Model,
            migrationContext.GetService<IMigrationsAssembly>().ModelSnapshot!.Model,
            new BpMigration_20261010183037().TargetModel,
        };
        var expected = models[0].FindEntityType(typeof(BpRole))!.GetSeedData().OrderBy(role => role["Id"]).ToList();
        Assert.Equal(new[] { "Admin", "BasicUser", "SubscribedUserL1" }, expected.Select(role => role["RoleName"]));
        foreach (var model in models.Skip(1))
        {
            var actual = model.FindEntityType(typeof(BpRole).FullName!)!.GetSeedData().OrderBy(role => role["Id"]).ToList();
            Assert.Equal(expected.Count, actual.Count);
            for (var index = 0; index < expected.Count; index++)
            {
                foreach (var property in model.FindEntityType(typeof(BpRole).FullName!)!.GetProperties())
                {
                    Assert.Equal(expected[index][property.Name], actual[index][property.Name]);
                }
            }
        }
    }
}
