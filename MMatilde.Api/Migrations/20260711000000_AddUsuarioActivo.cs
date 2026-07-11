using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;

#nullable disable

namespace MMatilde.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260711000000_AddUsuarioActivo")]
public partial class AddUsuarioActivo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "activo",
            table: "usuarios",
            type: "boolean",
            nullable: false,
            defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "activo",
            table: "usuarios");
    }
}
