#pragma warning disable SA1137 // ElementsShouldHaveTheSameIndentation
#pragma warning disable SA1413 // Use trailing comma in multi-line initializers
﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbMigration.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class BpMigration_20261011102323 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginProviders", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "LoginProviders",
                column: "ProviderName",
                values: new object[,]
                {
                    { "Google" },
                    { "Facebook" },
                });

            migrationBuilder.CreateTable(
                name: "BpUserLoginProviders",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProviderId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BpUserLoginProviders", x => new { x.UserId, x.ProviderId });
                    table.ForeignKey(
                        name: "FK_BpUserLoginProviders_BpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "BpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BpUserLoginProviders_LoginProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "LoginProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BpUserLoginProviders_ProviderId",
                table: "BpUserLoginProviders",
                column: "ProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BpUserLoginProviders");

            migrationBuilder.DropTable(
                name: "LoginProviders");
        }
    }
}
