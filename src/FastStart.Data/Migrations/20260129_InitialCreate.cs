using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastStart.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Applications",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                ExecutablePath = table.Column<string>(type: "TEXT", nullable: false),
                Arguments = table.Column<string>(type: "TEXT", nullable: true),
                WorkingDirectory = table.Column<string>(type: "TEXT", nullable: true),
                IconPath = table.Column<string>(type: "TEXT", nullable: true),
                Source = table.Column<string>(type: "TEXT", nullable: false),
                PackageFamilyName = table.Column<string>(type: "TEXT", nullable: true),
                LastIndexedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Applications", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Preferences",
            columns: table => new
            {
                Key = table.Column<string>(type: "TEXT", nullable: false),
                Value = table.Column<string>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Preferences", x => x.Key);
            });

        migrationBuilder.CreateTable(
            name: "SchemaVersion",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SchemaVersion", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AppTokens",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AppId = table.Column<long>(type: "INTEGER", nullable: false),
                Token = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AppTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_AppTokens_Applications_AppId",
                    column: x => x.AppId,
                    principalTable: "Applications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RecentLaunches",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ApplicationId = table.Column<long>(type: "INTEGER", nullable: false),
                LaunchedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                SearchQuery = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecentLaunches", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UserPins",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ApplicationId = table.Column<long>(type: "INTEGER", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false),
                GroupName = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserPins", x => x.Id);
            });

        migrationBuilder.InsertData(
            table: "SchemaVersion",
            columns: new[] { "Id", "UpdatedAtUtc", "Version" },
            values: new object[] { 1, new DateTimeOffset(2026, 1, 29, 0, 0, 0, TimeSpan.Zero), 1 });

        migrationBuilder.CreateIndex(
            name: "idx_apps_name",
            table: "Applications",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_Applications_ExecutablePath_Arguments",
            table: "Applications",
            columns: new[] { "ExecutablePath", "Arguments" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "idx_tokens_token",
            table: "AppTokens",
            column: "Token");

        migrationBuilder.CreateIndex(
            name: "IX_AppTokens_AppId",
            table: "AppTokens",
            column: "AppId");

        migrationBuilder.CreateIndex(
            name: "idx_recent_app",
            table: "RecentLaunches",
            column: "ApplicationId");

        migrationBuilder.CreateIndex(
            name: "idx_pins_position",
            table: "UserPins",
            column: "Position");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AppTokens");

        migrationBuilder.DropTable(
            name: "Preferences");

        migrationBuilder.DropTable(
            name: "RecentLaunches");

        migrationBuilder.DropTable(
            name: "SchemaVersion");

        migrationBuilder.DropTable(
            name: "UserPins");

        migrationBuilder.DropTable(
            name: "Applications");
    }
}
