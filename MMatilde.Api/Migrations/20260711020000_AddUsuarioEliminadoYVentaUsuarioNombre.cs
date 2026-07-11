using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;

#nullable disable

namespace MMatilde.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260711020000_AddUsuarioEliminadoYVentaUsuarioNombre")]
public partial class AddUsuarioEliminadoYVentaUsuarioNombre : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "eliminado_en",
            table: "usuarios",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.DropIndex(
            name: "IX_usuarios_email",
            table: "usuarios");

        migrationBuilder.CreateIndex(
            name: "IX_usuarios_email",
            table: "usuarios",
            column: "email",
            unique: true,
            filter: "eliminado_en IS NULL");

        migrationBuilder.AddColumn<string>(
            name: "usuario_nombre",
            table: "ventas",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE ventas v
            SET usuario_nombre = u.nombre
            FROM usuarios u
            WHERE v.usuario_id = u.id AND v.usuario_nombre IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "usuario_nombre",
            table: "ventas");

        migrationBuilder.DropIndex(
            name: "IX_usuarios_email",
            table: "usuarios");

        migrationBuilder.CreateIndex(
            name: "IX_usuarios_email",
            table: "usuarios",
            column: "email",
            unique: true);

        migrationBuilder.DropColumn(
            name: "eliminado_en",
            table: "usuarios");
    }
}
