using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ce.Gateway.Api.Migrations
{
    /// <inheritdoc />
    public partial class addRequestBodyToRequestLogEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestBody",
                table: "OcrGatewayLogEntries",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestBody",
                table: "OcrGatewayLogEntries");
        }
    }
}
