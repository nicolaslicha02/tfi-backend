using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionReservas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaActorSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioEmail",
                table: "Auditorias",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioNombre",
                table: "Auditorias",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsuarioEmail",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "UsuarioNombre",
                table: "Auditorias");
        }
    }
}
