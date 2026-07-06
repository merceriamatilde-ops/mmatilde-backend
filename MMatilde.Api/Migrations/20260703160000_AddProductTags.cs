using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace MMatilde.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703160000_AddProductTags")]
    public partial class AddProductTags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    color_hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    visible_en_catalogo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    orden = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "producto_tags",
                columns: table => new
                {
                    producto_id = table.Column<int>(type: "integer", nullable: false),
                    tag_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producto_tags", x => new { x.producto_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_producto_tags_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_producto_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_tags_nombre", table: "tags", column: "nombre", unique: true);
            migrationBuilder.CreateIndex(name: "IX_tags_slug", table: "tags", column: "slug", unique: true);
            migrationBuilder.CreateIndex(name: "IX_producto_tags_tag_id", table: "producto_tags", column: "tag_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "producto_tags");
            migrationBuilder.DropTable(name: "tags");
        }
    }
}
