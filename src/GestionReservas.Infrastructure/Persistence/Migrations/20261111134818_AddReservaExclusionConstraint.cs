using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionReservas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservaExclusionConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Defensa en BD: impide dos reservas APROBADAS (Estado = 3) superpuestas en el mismo espacio.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Reservas""
                ADD CONSTRAINT ""EX_Reservas_NoOverlap_Aprobadas""
                EXCLUDE USING gist (
                    ""EspacioId"" WITH =,
                    tstzrange(""FechaHoraInicio"", ""FechaHoraFin"", '[)') WITH &&
                )
                WHERE (""Estado"" = 3);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Reservas"" DROP CONSTRAINT IF EXISTS ""EX_Reservas_NoOverlap_Aprobadas"";");
        }
    }
}
