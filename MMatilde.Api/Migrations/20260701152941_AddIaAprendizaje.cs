using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MMatilde.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIaAprendizaje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ia_consultas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proyecto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tecnica = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contexto_json = table.Column<string>(type: "jsonb", nullable: false),
                    resultado_json = table.Column<string>(type: "jsonb", nullable: false),
                    productos_json = table.Column<string>(type: "jsonb", nullable: true),
                    evaluacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    nota_correccion = table.Column<string>(type: "text", nullable: true),
                    correccion_esperada = table.Column<string>(type: "text", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    revisado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ia_consultas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ia_reglas_aprendidas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    disparadores = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    regla = table.Column<string>(type: "text", nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    consulta_origen_id = table.Column<int>(type: "integer", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ia_reglas_aprendidas", x => x.id);
                    table.ForeignKey(
                        name: "FK_ia_reglas_aprendidas_ia_consultas_consulta_origen_id",
                        column: x => x.consulta_origen_id,
                        principalTable: "ia_consultas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ia_consultas_creado_en",
                table: "ia_consultas",
                column: "creado_en");

            migrationBuilder.CreateIndex(
                name: "IX_ia_consultas_evaluacion",
                table: "ia_consultas",
                column: "evaluacion");

            migrationBuilder.CreateIndex(
                name: "IX_ia_reglas_aprendidas_activa",
                table: "ia_reglas_aprendidas",
                column: "activa");

            migrationBuilder.CreateIndex(
                name: "IX_ia_reglas_aprendidas_consulta_origen_id",
                table: "ia_reglas_aprendidas",
                column: "consulta_origen_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ia_reglas_aprendidas");

            migrationBuilder.DropTable(
                name: "ia_consultas");
        }
    }
}
