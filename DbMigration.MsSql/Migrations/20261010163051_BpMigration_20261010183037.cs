#pragma warning disable SA1137 // ElementsShouldHaveTheSameIndentation
#pragma warning disable SA1413 // Use trailing comma in multi-line initializers
﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbMigration.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class BpMigration_20261010183037 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var roles = new object[,]
            {
                { 2, "BasicUser", "Basic user role", true, new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc), 1 },
                { 3, "SubscribedUserL1", "Level 1 subscribed user role", true, new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc), 1 }
            };

            migrationBuilder.InsertData(
                table: "BpRoles",
                columns: new[] { "Id", "RoleName", "Description", "IsActive", "CreatedDate", "CreatedByUserId", "ModifiedDate", "ModifiedByUserId" },
                values: roles);

            migrationBuilder.DropForeignKey(
                name: "FK_Competition_BpUsers_ModifiedByUserId",
                table: "Competition");

            migrationBuilder.DropForeignKey(
                name: "FK_Competition_Countries_CountryId",
                table: "Competition");

            migrationBuilder.DropForeignKey(
                name: "FK_Competition_Sports_SportId",
                table: "Competition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Competition",
                table: "Competition");

            migrationBuilder.RenameTable(
                name: "Competition",
                newName: "Competitions");

            migrationBuilder.RenameIndex(
                name: "IX_Competition_SportId",
                table: "Competitions",
                newName: "IX_Competitions_SportId");

            migrationBuilder.RenameIndex(
                name: "IX_Competition_ModifiedByUserId",
                table: "Competitions",
                newName: "IX_Competitions_ModifiedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Competition_CountryId",
                table: "Competitions",
                newName: "IX_Competitions_CountryId");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Competitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Competitions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Competitions",
                table: "Competitions",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Competitors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WebSiteUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Competitors_BpUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "BpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Competitors_BpUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "BpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BpUserFavorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    SportId = table.Column<int>(type: "int", nullable: true),
                    CompetitionId = table.Column<int>(type: "int", nullable: true),
                    CompetitorId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BpUserFavorites", x => x.Id);
                    table.CheckConstraint("CK_BpUserFavorites_ExactlyOneTarget", "(CASE WHEN CountryId IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN SportId IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN CompetitionId IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN CompetitorId IS NOT NULL THEN 1 ELSE 0 END) = 1");
                    table.ForeignKey(
                        name: "FK_BpUserFavorites_BpUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "BpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BpUserFavorites_BpUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "BpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BpUserFavorites_BpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "BpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BpUserFavorites_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BpUserFavorites_Competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "Competitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BpUserFavorites_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BpUserFavorites_Sports_SportId",
                        column: x => x.SportId,
                        principalTable: "Sports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_CreatedByUserId",
                table: "Competitions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BpUserFavorites_CompetitionId",
                table: "BpUserFavorites",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_BpUserFavorites_CompetitorId",
                table: "BpUserFavorites",
                column: "CompetitorId");

            migrationBuilder.CreateIndex(
                name: "IX_BpUserFavorites_CountryId",
                table: "BpUserFavorites",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_BpUserFavorites_CreatedByUserId",
                table: "BpUserFavorites",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BpUserFavorites_ModifiedByUserId",
                table: "BpUserFavorites",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BpUserFavorites_SportId",
                table: "BpUserFavorites",
                column: "SportId");

            migrationBuilder.CreateIndex(
                name: "IX_BpUserFavorites_UserId_CompetitionId",
                table: "BpUserFavorites",
                columns: new[] { "UserId", "CompetitionId" },
                unique: true,
                filter: "[CompetitionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BpUserFavorites_UserId_CompetitorId",
                table: "BpUserFavorites",
                columns: new[] { "UserId", "CompetitorId" },
                unique: true,
                filter: "[CompetitorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BpUserFavorites_UserId_CountryId",
                table: "BpUserFavorites",
                columns: new[] { "UserId", "CountryId" },
                unique: true,
                filter: "[CountryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BpUserFavorites_UserId_SportId",
                table: "BpUserFavorites",
                columns: new[] { "UserId", "SportId" },
                unique: true,
                filter: "[SportId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Competitors_CreatedByUserId",
                table: "Competitors",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Competitors_ModifiedByUserId",
                table: "Competitors",
                column: "ModifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Competitions_BpUsers_CreatedByUserId",
                table: "Competitions",
                column: "CreatedByUserId",
                principalTable: "BpUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Competitions_BpUsers_ModifiedByUserId",
                table: "Competitions",
                column: "ModifiedByUserId",
                principalTable: "BpUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Competitions_Countries_CountryId",
                table: "Competitions",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Competitions_Sports_SportId",
                table: "Competitions",
                column: "SportId",
                principalTable: "Sports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BpRoles",
                keyColumn: "Id",
                keyValues: new object[] { 2, 3 });

            migrationBuilder.DropForeignKey(
                name: "FK_Competitions_BpUsers_CreatedByUserId",
                table: "Competitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Competitions_BpUsers_ModifiedByUserId",
                table: "Competitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Competitions_Countries_CountryId",
                table: "Competitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Competitions_Sports_SportId",
                table: "Competitions");

            migrationBuilder.DropTable(
                name: "BpUserFavorites");

            migrationBuilder.DropTable(
                name: "Competitors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Competitions",
                table: "Competitions");

            migrationBuilder.DropIndex(
                name: "IX_Competitions_CreatedByUserId",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Competitions");

            migrationBuilder.RenameTable(
                name: "Competitions",
                newName: "Competition");

            migrationBuilder.RenameIndex(
                name: "IX_Competitions_SportId",
                table: "Competition",
                newName: "IX_Competition_SportId");

            migrationBuilder.RenameIndex(
                name: "IX_Competitions_ModifiedByUserId",
                table: "Competition",
                newName: "IX_Competition_ModifiedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Competitions_CountryId",
                table: "Competition",
                newName: "IX_Competition_CountryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Competition",
                table: "Competition",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Competition_BpUsers_ModifiedByUserId",
                table: "Competition",
                column: "ModifiedByUserId",
                principalTable: "BpUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Competition_Countries_CountryId",
                table: "Competition",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Competition_Sports_SportId",
                table: "Competition",
                column: "SportId",
                principalTable: "Sports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
