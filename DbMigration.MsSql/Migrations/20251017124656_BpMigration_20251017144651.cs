#pragma warning disable SA1137 // ElementsShouldHaveTheSameIndentation
#pragma warning disable SA1413 // Use trailing comma in multi-line initializers
﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbMigration.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class BpMigration_20251017144651 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BpRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BpRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BpUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BpUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BpRoleBpUser",
                columns: table => new
                {
                    BpRolesId = table.Column<int>(type: "int", nullable: false),
                    BpUsersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BpRoleBpUser", x => new { x.BpRolesId, x.BpUsersId });
                    table.ForeignKey(
                        name: "FK_BpRoleBpUser_BpRoles_BpRolesId",
                        column: x => x.BpRolesId,
                        principalTable: "BpRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BpRoleBpUser_BpUsers_BpUsersId",
                        column: x => x.BpUsersId,
                        principalTable: "BpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UserInitToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserTokens_BpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "BpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BpRoleBpUser_BpUsersId",
                table: "BpRoleBpUser",
                column: "BpUsersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BpRoleBpUser");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "BpRoles");

            migrationBuilder.DropTable(
                name: "BpUsers");
        }
    }
}
