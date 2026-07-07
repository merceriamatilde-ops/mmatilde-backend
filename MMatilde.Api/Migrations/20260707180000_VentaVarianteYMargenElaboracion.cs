using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;

#nullable disable

namespace MMatilde.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260707180000_VentaVarianteYMargenElaboracion")]
public partial class VentaVarianteYMargenElaboracion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "margen_elaboracion_monto",
            table: "productos",
            type: "numeric(18,2)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "margen_elaboracion_porcentaje",
            table: "productos",
            type: "numeric(5,2)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "variante_id",
            table: "venta_lineas",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "variante_label",
            table: "venta_lineas",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_venta_lineas_variante_id",
            table: "venta_lineas",
            column: "variante_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_venta_lineas_variante_id", table: "venta_lineas");
        migrationBuilder.DropColumn(name: "variante_label", table: "venta_lineas");
        migrationBuilder.DropColumn(name: "variante_id", table: "venta_lineas");
        migrationBuilder.DropColumn(name: "margen_elaboracion_porcentaje", table: "productos");
        migrationBuilder.DropColumn(name: "margen_elaboracion_monto", table: "productos");
    }
}
