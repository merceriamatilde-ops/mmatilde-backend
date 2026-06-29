using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMatilde.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductoRelacionados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "producto_relacionados",
                columns: table => new
                {
                    producto_principal_id = table.Column<int>(type: "integer", nullable: false),
                    producto_vinculado_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producto_relacionados", x => new { x.producto_principal_id, x.producto_vinculado_id });
                    table.ForeignKey(
                        name: "FK_producto_relacionados_productos_producto_principal_id",
                        column: x => x.producto_principal_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_producto_relacionados_productos_producto_vinculado_id",
                        column: x => x.producto_vinculado_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_producto_relacionados_producto_vinculado_id",
                table: "producto_relacionados",
                column: "producto_vinculado_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "producto_relacionados");
        }
    }
}
