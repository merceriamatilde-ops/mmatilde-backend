using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;

#nullable disable

namespace MMatilde.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260710150000_ProductoDeleteSetNull")]
public partial class ProductoDeleteSetNull : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_venta_lineas_productos_producto_id",
            table: "venta_lineas");

        migrationBuilder.AlterColumn<int>(
            name: "producto_id",
            table: "venta_lineas",
            type: "integer",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AddForeignKey(
            name: "FK_venta_lineas_productos_producto_id",
            table: "venta_lineas",
            column: "producto_id",
            principalTable: "productos",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.DropForeignKey(
            name: "FK_producto_relacionados_productos_producto_vinculado_id",
            table: "producto_relacionados");

        migrationBuilder.AddForeignKey(
            name: "FK_producto_relacionados_productos_producto_vinculado_id",
            table: "producto_relacionados",
            column: "producto_vinculado_id",
            principalTable: "productos",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_venta_lineas_productos_producto_id",
            table: "venta_lineas");

        migrationBuilder.AlterColumn<int>(
            name: "producto_id",
            table: "venta_lineas",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);

        migrationBuilder.AddForeignKey(
            name: "FK_venta_lineas_productos_producto_id",
            table: "venta_lineas",
            column: "producto_id",
            principalTable: "productos",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.DropForeignKey(
            name: "FK_producto_relacionados_productos_producto_vinculado_id",
            table: "producto_relacionados");

        migrationBuilder.AddForeignKey(
            name: "FK_producto_relacionados_productos_producto_vinculado_id",
            table: "producto_relacionados",
            column: "producto_vinculado_id",
            principalTable: "productos",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }
}
