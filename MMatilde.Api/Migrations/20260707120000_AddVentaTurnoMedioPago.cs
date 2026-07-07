using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;

#nullable disable

namespace MMatilde.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260707120000_AddVentaTurnoMedioPago")]
public partial class AddVentaTurnoMedioPago : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "turno",
            table: "ventas",
            type: "text",
            nullable: false,
            defaultValue: "MANANA");

        migrationBuilder.AddColumn<string>(
            name: "medio_pago",
            table: "ventas",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ganancia_neta_estimada",
            table: "ventas",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.CreateIndex(
            name: "IX_ventas_turno",
            table: "ventas",
            column: "turno");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_ventas_turno", table: "ventas");
        migrationBuilder.DropColumn(name: "turno", table: "ventas");
        migrationBuilder.DropColumn(name: "medio_pago", table: "ventas");
        migrationBuilder.DropColumn(name: "ganancia_neta_estimada", table: "ventas");
    }
}
