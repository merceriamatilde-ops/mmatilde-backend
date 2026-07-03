using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace MMatilde.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260702120000_AddProductoContenidoPublico")]
    public partial class AddProductoContenidoPublico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "nombre_publico",
                table: "productos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "descripcion_publica",
                table: "productos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imagen_publica_url",
                table: "productos",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "es_de_proveedor",
                table: "producto_imagenes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE producto_imagenes pi
                SET es_de_proveedor = true
                FROM productos p
                INNER JOIN proveedores pr ON p.proveedor_id = pr.id
                WHERE pi.producto_id = p.id AND pr.slug = 'makor';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "nombre_publico",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "descripcion_publica",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "imagen_publica_url",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "es_de_proveedor",
                table: "producto_imagenes");
        }
    }
}
