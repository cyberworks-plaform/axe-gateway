using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ce.Gateway.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OcrGatewayLogEntries_CreatedAtUtc_DownstreamStatusCode",
                table: "OcrGatewayLogEntries",
                columns: new[] { "CreatedAtUtc", "DownstreamStatusCode" });

            migrationBuilder.CreateIndex(
                name: "IX_OcrGatewayLogEntries_CreatedAtUtc_IsError",
                table: "OcrGatewayLogEntries",
                columns: new[] { "CreatedAtUtc", "IsError" });

            migrationBuilder.CreateIndex(
                name: "IX_OcrGatewayLogEntries_UpstreamHost",
                table: "OcrGatewayLogEntries",
                column: "UpstreamHost");

            migrationBuilder.CreateIndex(
                name: "IX_OcrGatewayLogEntries_UpstreamPath",
                table: "OcrGatewayLogEntries",
                column: "UpstreamPath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OcrGatewayLogEntries_CreatedAtUtc_DownstreamStatusCode",
                table: "OcrGatewayLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_OcrGatewayLogEntries_CreatedAtUtc_IsError",
                table: "OcrGatewayLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_OcrGatewayLogEntries_UpstreamHost",
                table: "OcrGatewayLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_OcrGatewayLogEntries_UpstreamPath",
                table: "OcrGatewayLogEntries");
        }
    }
}
