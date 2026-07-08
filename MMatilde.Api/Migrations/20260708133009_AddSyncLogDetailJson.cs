using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMatilde.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncLogDetailJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "categorias_json",
                table: "sync_logs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resumen_json",
                table: "sync_logs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "terms_json",
                table: "sync_logs",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "categorias_json",
                table: "sync_logs");

            migrationBuilder.DropColumn(
                name: "resumen_json",
                table: "sync_logs");

            migrationBuilder.DropColumn(
                name: "terms_json",
                table: "sync_logs");
        }
    }
}
