using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionReservas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaRolHistorico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "Auditorias",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Accion",
                table: "Auditorias",
                column: "Accion");

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_FechaHora_Id",
                table: "Auditorias",
                columns: new[] { "FechaHora", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Auditorias_Accion",
                table: "Auditorias");

            migrationBuilder.DropIndex(
                name: "IX_Auditorias_FechaHora_Id",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Auditorias");
        }
    }
}
