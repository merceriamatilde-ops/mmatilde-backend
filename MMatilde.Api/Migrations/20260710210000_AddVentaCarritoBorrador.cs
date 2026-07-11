using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MMatilde.Api.Data;

#nullable disable

namespace MMatilde.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260710210000_AddVentaCarritoBorrador")]
public partial class AddVentaCarritoBorrador : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "venta_carritos_borrador",
            columns: table => new
            {
                usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                payload_json = table.Column<string>(type: "jsonb", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_venta_carritos_borrador", x => x.usuario_id);
                table.ForeignKey(
                    name: "FK_venta_carritos_borrador_usuarios_usuario_id",
                    column: x => x.usuario_id,
                    principalTable: "usuarios",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "venta_carritos_borrador");
    }
}
