#pragma warning disable SA1137 // ElementsShouldHaveTheSameIndentation
#pragma warning disable SA1413 // Use trailing comma in multi-line initializers
﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbMigration.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class BpMigration_20261017192338 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename in place. Dropping these tables discards activation tokens and saved grid layouts.
            migrationBuilder.RenameTable(
                name: "UserGridSettings",
                newName: "BpUserGridSettings");

            migrationBuilder.RenameTable(
                name: "UserTokens",
                newName: "BpUserTokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "BpUserGridSettings",
                newName: "UserGridSettings");

            migrationBuilder.RenameTable(
                name: "BpUserTokens",
                newName: "UserTokens");
        }
    }
}
