using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DbMigration.MsSql.Migrations
{
    [DbContext(typeof(BpDbContext))]
    [Migration("20261024210000_AddMicrosoftLoginProvider")]
    public sealed class AddMicrosoftLoginProvider : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "LoginProviders",
                columns: new[] { "ProviderName" },
                columnTypes: new[] { "nvarchar(50)" },
                values: new object[] { "Microsoft" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "LoginProviders",
                keyColumn: "ProviderName",
                keyColumnType: "nvarchar(50)",
                keyValue: "Microsoft");
        }
    }
}
