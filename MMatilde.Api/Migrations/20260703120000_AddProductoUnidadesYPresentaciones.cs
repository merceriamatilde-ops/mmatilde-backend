using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace MMatilde.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703120000_AddProductoUnidadesYPresentaciones")]
    public partial class AddProductoUnidadesYPresentaciones : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "unidad_base",
                table: "productos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cantidad_unidad_compra",
                table: "productos",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "etiqueta_unidad_compra",
                table: "productos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "unidad_compra_auto_detectada",
                table: "productos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "producto_presentaciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    producto_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    cantidad_unidad_base = table.Column<decimal>(type: "numeric(18,6)", nullable: false, defaultValue: 1m),
                    precio_venta = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    margen_porcentaje = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    es_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    orden = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producto_presentaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_producto_presentaciones_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_producto_presentaciones_producto_id",
                table: "producto_presentaciones",
                column: "producto_id");

            migrationBuilder.Sql(@"
                INSERT INTO configuracion_sitio (clave, valor, tipo, grupo, label, orden, updated_at)
                SELECT 'precio_iva_porcentaje', '21', 'number', 'precios', 'IVA %', 1, now()
                WHERE NOT EXISTS (SELECT 1 FROM configuracion_sitio WHERE clave = 'precio_iva_porcentaje');

                INSERT INTO configuracion_sitio (clave, valor, tipo, grupo, label, orden, updated_at)
                SELECT 'precio_margen_global', '115', 'number', 'precios', 'Margen global %', 2, now()
                WHERE NOT EXISTS (SELECT 1 FROM configuracion_sitio WHERE clave = 'precio_margen_global');
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "producto_presentaciones");

            migrationBuilder.DropColumn(name: "unidad_base", table: "productos");
            migrationBuilder.DropColumn(name: "cantidad_unidad_compra", table: "productos");
            migrationBuilder.DropColumn(name: "etiqueta_unidad_compra", table: "productos");
            migrationBuilder.DropColumn(name: "unidad_compra_auto_detectada", table: "productos");

            migrationBuilder.Sql(@"
                DELETE FROM configuracion_sitio WHERE clave IN ('precio_iva_porcentaje', 'precio_margen_global');
            ");
        }
    }
}
