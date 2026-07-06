using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;

#nullable disable

namespace MMatilde.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260706180000_AddModoOrigenEconomicoVentas")]
public partial class AddModoOrigenEconomicoVentas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "modo_origen_economico",
            table: "productos",
            type: "text",
            nullable: false,
            defaultValue: "REVENTA");

        migrationBuilder.AddColumn<decimal>(
            name: "comision_tienda_porcentaje",
            table: "productos",
            type: "numeric(5,2)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "titular_consignacion",
            table: "productos",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "costo_materiales",
            table: "productos",
            type: "numeric(18,2)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "mano_obra",
            table: "productos",
            type: "numeric(18,2)",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "ventas",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                notas = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table => table.PrimaryKey("PK_ventas", x => x.id));

        migrationBuilder.CreateTable(
            name: "venta_lineas",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                venta_id = table.Column<int>(type: "integer", nullable: false),
                producto_id = table.Column<int>(type: "integer", nullable: false),
                producto_nombre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                cantidad = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                precio_unitario_venta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                modo_origen_economico = table.Column<string>(type: "text", nullable: false),
                costo_compra_snapshot = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                costo_materiales_snapshot = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                mano_obra_snapshot = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                comision_tienda_porcentaje_snapshot = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                ganancia_neta_estimada = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_venta_lineas", x => x.id);
                table.ForeignKey(
                    name: "FK_venta_lineas_productos_producto_id",
                    column: x => x.producto_id,
                    principalTable: "productos",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_venta_lineas_ventas_venta_id",
                    column: x => x.venta_id,
                    principalTable: "ventas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_venta_lineas_producto_id", table: "venta_lineas", column: "producto_id");
        migrationBuilder.CreateIndex(name: "IX_venta_lineas_venta_id", table: "venta_lineas", column: "venta_id");
        migrationBuilder.CreateIndex(name: "IX_ventas_fecha", table: "ventas", column: "fecha");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "venta_lineas");
        migrationBuilder.DropTable(name: "ventas");
        migrationBuilder.DropColumn(name: "modo_origen_economico", table: "productos");
        migrationBuilder.DropColumn(name: "comision_tienda_porcentaje", table: "productos");
        migrationBuilder.DropColumn(name: "titular_consignacion", table: "productos");
        migrationBuilder.DropColumn(name: "costo_materiales", table: "productos");
        migrationBuilder.DropColumn(name: "mano_obra", table: "productos");
    }
}
