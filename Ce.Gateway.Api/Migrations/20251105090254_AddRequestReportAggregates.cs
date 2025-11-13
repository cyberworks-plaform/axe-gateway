using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ce.Gateway.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestReportAggregates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestReportAggregates",
                columns: table => new
                {
                    PeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Granularity = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    StatusCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<long>(type: "INTEGER", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestReportAggregates", x => new { x.PeriodStart, x.Granularity, x.StatusCategory });
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestReportAggregates_PeriodStart",
                table: "RequestReportAggregates",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_RequestReportAggregates_PeriodStart_Granularity",
                table: "RequestReportAggregates",
                columns: new[] { "PeriodStart", "Granularity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestReportAggregates");
        }
    }
}
