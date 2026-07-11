using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;

#nullable disable

namespace MMatilde.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260711010000_AddVentaUsuarioId")]
public partial class AddVentaUsuarioId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "usuario_id",
            table: "ventas",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ventas_usuario_id",
            table: "ventas",
            column: "usuario_id");

        migrationBuilder.AddForeignKey(
            name: "FK_ventas_usuarios_usuario_id",
            table: "ventas",
            column: "usuario_id",
            principalTable: "usuarios",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ventas_usuarios_usuario_id",
            table: "ventas");

        migrationBuilder.DropIndex(
            name: "IX_ventas_usuario_id",
            table: "ventas");

        migrationBuilder.DropColumn(
            name: "usuario_id",
            table: "ventas");
    }
}
