using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;

#nullable disable

namespace MMatilde.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260707140000_AddMediosPago")]
public partial class AddMediosPago : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "medios_pago",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                es_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                orden = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table => table.PrimaryKey("PK_medios_pago", x => x.id));

        migrationBuilder.CreateIndex(name: "IX_medios_pago_slug", table: "medios_pago", column: "slug", unique: true);

        migrationBuilder.Sql("""
            INSERT INTO medios_pago (nombre, slug, activo, es_default, orden, created_at, updated_at) VALUES
            ('Efectivo', 'efectivo', true, true, 1, now(), now()),
            ('Transferencia', 'transferencia', true, false, 2, now(), now()),
            ('Mixto', 'mixto', true, false, 3, now(), now());
            """);

        migrationBuilder.Sql("""
            UPDATE ventas SET medio_pago = LOWER(medio_pago) WHERE medio_pago IS NOT NULL;
            UPDATE ventas SET medio_pago = 'efectivo' WHERE medio_pago IS NULL OR medio_pago = '';
            """);

        migrationBuilder.AlterColumn<string>(
            name: "medio_pago",
            table: "ventas",
            type: "character varying(120)",
            maxLength: 120,
            nullable: false,
            defaultValue: "efectivo",
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "medio_pago",
            table: "ventas",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(120)",
            oldMaxLength: 120);

        migrationBuilder.DropTable(name: "medios_pago");
    }
}
