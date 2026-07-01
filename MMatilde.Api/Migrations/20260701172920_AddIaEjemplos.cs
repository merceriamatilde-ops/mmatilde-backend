using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MMatilde.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIaEjemplos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ia_ejemplos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    disparadores = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    respuesta_json = table.Column<string>(type: "jsonb", nullable: false),
                    imagen_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ia_ejemplos", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ia_ejemplos_activa",
                table: "ia_ejemplos",
                column: "activa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ia_ejemplos");
        }
    }
}
