using DbMigration.MsSql.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

public sealed class UserTableRenameMigrationTests
{
    [Fact]
    public void Rename_migration_keeps_activation_tokens_and_grid_settings()
    {
        var migration = new BpMigration_20261017192338();

        AssertRenames(
            migration.UpOperations,
            ("UserGridSettings", "BpUserGridSettings"),
            ("UserTokens", "BpUserTokens"));
        AssertRenames(
            migration.DownOperations,
            ("BpUserGridSettings", "UserGridSettings"),
            ("BpUserTokens", "UserTokens"));
    }

    private static void AssertRenames(
        IReadOnlyList<MigrationOperation> operations,
        params (string Name, string NewName)[] expected)
    {
        Assert.Equal(expected.Length, operations.Count);
        Assert.All(operations, operation => Assert.IsType<RenameTableOperation>(operation));
        for (var index = 0; index < expected.Length; index++)
        {
            var rename = (RenameTableOperation)operations[index];
            Assert.Null(rename.Schema);
            Assert.Equal(expected[index].Name, rename.Name);
            Assert.Equal(expected[index].NewName, rename.NewName);
        }
    }
}
