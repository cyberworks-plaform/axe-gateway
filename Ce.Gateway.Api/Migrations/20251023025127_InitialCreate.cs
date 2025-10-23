using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ce.Gateway.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OcrGatewayLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TraceId = table.Column<string>(type: "TEXT", nullable: true),
                    UpstreamHost = table.Column<string>(type: "TEXT", nullable: true),
                    UpstreamPort = table.Column<int>(type: "INTEGER", nullable: true),
                    UpstreamScheme = table.Column<string>(type: "TEXT", nullable: true),
                    UpstreamHttpMethod = table.Column<string>(type: "TEXT", nullable: true),
                    UpstreamPath = table.Column<string>(type: "TEXT", nullable: true),
                    UpstreamQueryString = table.Column<string>(type: "TEXT", nullable: true),
                    UpstreamRequestSize = table.Column<long>(type: "INTEGER", nullable: true),
                    UpstreamClientIp = table.Column<string>(type: "TEXT", nullable: true),
                    DownstreamScheme = table.Column<string>(type: "TEXT", nullable: true),
                    DownstreamHost = table.Column<string>(type: "TEXT", nullable: true),
                    DownstreamPort = table.Column<int>(type: "INTEGER", nullable: true),
                    DownstreamPath = table.Column<string>(type: "TEXT", nullable: true),
                    DownstreamQueryString = table.Column<string>(type: "TEXT", nullable: true),
                    DownstreamRequestSize = table.Column<long>(type: "INTEGER", nullable: true),
                    DownstreamResponseSize = table.Column<long>(type: "INTEGER", nullable: true),
                    DownstreamStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    GatewayLatencyMs = table.Column<long>(type: "INTEGER", nullable: false),
                    IsError = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrGatewayLogEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OcrGatewayLogEntries_CreatedAtUtc",
                table: "OcrGatewayLogEntries",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OcrGatewayLogEntries_DownstreamHost",
                table: "OcrGatewayLogEntries",
                column: "DownstreamHost");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OcrGatewayLogEntries");
        }
    }
}
