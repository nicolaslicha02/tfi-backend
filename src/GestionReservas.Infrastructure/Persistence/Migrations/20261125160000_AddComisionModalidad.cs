using GestionReservas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionReservas.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261125160000_AddComisionModalidad")]
public sealed class AddComisionModalidad : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // IF NOT EXISTS mantiene compatible el despliegue con bases donde la modalidad
        // ya fue agregada por la migración del panel académico de la rama de trabajo.
        migrationBuilder.Sql("ALTER TABLE \"Comisiones\" ADD COLUMN IF NOT EXISTS \"Modalidad\" character varying(30) NOT NULL DEFAULT 'Presencial';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"Comisiones\" DROP COLUMN IF EXISTS \"Modalidad\";");
    }
}
