using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;

#nullable disable

namespace MMatilde.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260707190000_VentaLineaPresentacion")]
public partial class VentaLineaPresentacion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "presentacion_id",
            table: "venta_lineas",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "presentacion_nombre",
            table: "venta_lineas",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "presentacion_nombre", table: "venta_lineas");
        migrationBuilder.DropColumn(name: "presentacion_id", table: "venta_lineas");
    }
}
