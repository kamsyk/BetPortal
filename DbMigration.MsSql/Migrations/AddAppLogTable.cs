using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DbMigration.MsSql.Migrations
{
    /// <summary>
    /// Creates the application log table written by the API's Serilog MSSqlServer sink. The sink owns the table, so it is
    /// intentionally not part of the EF model and does not change the model snapshot.
    /// </summary>
    [DbContext(typeof(BpDbContext))]
    [Migration("20261025090000_AddAppLogTable")]
    public sealed class AddAppLogTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BpAppLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogEvent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SpanId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    SourceContext = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BpAppLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BpAppLogs_TimeStamp",
                table: "BpAppLogs",
                column: "TimeStamp");

            migrationBuilder.CreateIndex(
                name: "IX_BpAppLogs_TraceId",
                table: "BpAppLogs",
                column: "TraceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BpAppLogs");
        }
    }
}
