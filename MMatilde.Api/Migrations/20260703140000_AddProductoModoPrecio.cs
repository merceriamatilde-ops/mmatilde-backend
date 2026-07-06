using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace MMatilde.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703140000_AddProductoModoPrecio")]
    public partial class AddProductoModoPrecio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "modo_precio",
                table: "productos",
                type: "text",
                nullable: false,
                defaultValue: "AUTOMATICO");

            migrationBuilder.AddColumn<decimal>(
                name: "iva_porcentaje_producto",
                table: "productos",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "margen_porcentaje_producto",
                table: "productos",
                type: "numeric(5,2)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "modo_precio", table: "productos");
            migrationBuilder.DropColumn(name: "iva_porcentaje_producto", table: "productos");
            migrationBuilder.DropColumn(name: "margen_porcentaje_producto", table: "productos");
        }
    }
}
