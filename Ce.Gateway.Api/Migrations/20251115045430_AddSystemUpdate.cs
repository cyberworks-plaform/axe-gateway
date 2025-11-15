using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ce.Gateway.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    GitHash = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: true),
                    Checksum = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DownloadUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReleaseNotes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DownloadedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InstallStartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InstallCompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InitiatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    BackupPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsCurrentVersion = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemUpdates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemUpdates_CreatedAt",
                table: "SystemUpdates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUpdates_IsCurrentVersion",
                table: "SystemUpdates",
                column: "IsCurrentVersion");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUpdates_Status",
                table: "SystemUpdates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUpdates_Version",
                table: "SystemUpdates",
                column: "Version");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemUpdates");
        }
    }
}
