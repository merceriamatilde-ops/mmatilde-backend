using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MMatilde.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBanners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTA: las columnas terms_json/categorias_json/resumen_json de sync_logs ya fueron
            // creadas por la migración AddSyncLogDetailJson. El snapshot quedó desincronizado y EF
            // las volvía a agregar acá; se omiten a propósito para no romper MigrateAsync.
            migrationBuilder.CreateTable(
                name: "banners",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    imagen_desktop_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    imagen_mobile_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    link_tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Ninguno"),
                    link_categoria_id = table.Column<int>(type: "integer", nullable: true),
                    link_tag_id = table.Column<int>(type: "integer", nullable: true),
                    link_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ubicacion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "home"),
                    orden = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    abre_en_nueva_pestana = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    fecha_desde = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fecha_hasta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_banners", x => x.id);
                    table.ForeignKey(
                        name: "FK_banners_categorias_link_categoria_id",
                        column: x => x.link_categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_banners_tags_link_tag_id",
                        column: x => x.link_tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_banners_link_categoria_id",
                table: "banners",
                column: "link_categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_banners_link_tag_id",
                table: "banners",
                column: "link_tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_banners_ubicacion_orden",
                table: "banners",
                columns: new[] { "ubicacion", "orden" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "banners");
        }
    }
}
