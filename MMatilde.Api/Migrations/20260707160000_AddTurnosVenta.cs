using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;

#nullable disable

namespace MMatilde.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260707160000_AddTurnosVenta")]
public partial class AddTurnosVenta : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "turnos_venta",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                orden = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                hora_desde = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table => table.PrimaryKey("PK_turnos_venta", x => x.id));

        migrationBuilder.CreateIndex(name: "IX_turnos_venta_slug", table: "turnos_venta", column: "slug", unique: true);

        migrationBuilder.Sql("""
            INSERT INTO turnos_venta (slug, nombre, orden, activo, hora_desde, created_at, updated_at) VALUES
            ('MANANA', 'Mañana', 1, true, '00:00:00', now(), now()),
            ('TARDE', 'Tarde', 2, true, '14:00:00', now(), now());
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "turnos_venta");
    }
}
