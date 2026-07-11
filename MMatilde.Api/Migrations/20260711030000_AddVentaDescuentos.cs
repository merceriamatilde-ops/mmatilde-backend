using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;

#nullable disable

namespace MMatilde.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260711030000_AddVentaDescuentos")]
public partial class AddVentaDescuentos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "subtotal_bruto",
            table: "ventas",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "descuento_global_porcentaje",
            table: "ventas",
            type: "numeric(5,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "descuento_global_monto",
            table: "ventas",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "subtotal_bruto",
            table: "venta_lineas",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "subtotal",
            table: "venta_lineas",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "descuento_porcentaje",
            table: "venta_lineas",
            type: "numeric(5,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "descuento_monto",
            table: "venta_lineas",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "descuento_global_asignado",
            table: "venta_lineas",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.Sql("""
            UPDATE venta_lineas
            SET subtotal_bruto = ROUND(cantidad * precio_unitario_venta, 2),
                subtotal = ROUND(cantidad * precio_unitario_venta, 2)
            WHERE subtotal = 0;

            UPDATE ventas v
            SET subtotal_bruto = COALESCE((
                SELECT SUM(l.subtotal_bruto) FROM venta_lineas l WHERE l.venta_id = v.id
            ), 0)
            WHERE subtotal_bruto = 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "descuento_global_asignado", table: "venta_lineas");
        migrationBuilder.DropColumn(name: "descuento_monto", table: "venta_lineas");
        migrationBuilder.DropColumn(name: "descuento_porcentaje", table: "venta_lineas");
        migrationBuilder.DropColumn(name: "subtotal", table: "venta_lineas");
        migrationBuilder.DropColumn(name: "subtotal_bruto", table: "venta_lineas");
        migrationBuilder.DropColumn(name: "descuento_global_monto", table: "ventas");
        migrationBuilder.DropColumn(name: "descuento_global_porcentaje", table: "ventas");
        migrationBuilder.DropColumn(name: "subtotal_bruto", table: "ventas");
    }
}
