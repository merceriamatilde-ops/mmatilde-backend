using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMatilde.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIaConsultaIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "ia_consultas",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ia_consultas_idempotency_key",
                table: "ia_consultas",
                column: "idempotency_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ia_consultas_idempotency_key",
                table: "ia_consultas");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "ia_consultas");
        }
    }
}
