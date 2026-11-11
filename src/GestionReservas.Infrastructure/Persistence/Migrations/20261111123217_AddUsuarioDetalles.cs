using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionReservas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioDetalles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Apellido",
                table: "Usuarios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DepartamentoId",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Usuarios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_DepartamentoId",
                table: "Usuarios",
                column: "DepartamentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Departamentos_DepartamentoId",
                table: "Usuarios",
                column: "DepartamentoId",
                principalTable: "Departamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Departamentos_DepartamentoId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_DepartamentoId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Apellido",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DepartamentoId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Usuarios");
        }
    }
}
