using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ce.Gateway.Api.Migrations
{
    public partial class InitGatewayDb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TraceId = table.Column<string>(type: "TEXT", nullable: true),
                    Route = table.Column<string>(type: "TEXT", nullable: true),
                    Method = table.Column<string>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    DownstreamNode = table.Column<string>(type: "TEXT", nullable: true),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: false),
                    LatencyMs = table.Column<long>(type: "INTEGER", nullable: false),
                    ServiceApi = table.Column<string>(type: "TEXT", nullable: true),
                    Client = table.Column<string>(type: "TEXT", nullable: true),
                    RequestSize = table.Column<long>(type: "INTEGER", nullable: false),
                    ResponseSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestLogEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogEntries_CreatedAtUtc",
                table: "RequestLogEntries",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogEntries_DownstreamNode",
                table: "RequestLogEntries",
                column: "DownstreamNode");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogEntries_Route",
                table: "RequestLogEntries",
                column: "Route");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestLogEntries");
        }
    }
}
