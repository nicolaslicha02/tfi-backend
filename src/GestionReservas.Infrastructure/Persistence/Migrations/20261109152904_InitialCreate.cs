using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestionReservas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CiclosLectivos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anio = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CiclosLectivos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipamientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipamientos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposEspacio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposEspacio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreCompleto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Materias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DepartamentoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Materias_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolPermisos",
                columns: table => new
                {
                    RolId = table.Column<int>(type: "integer", nullable: false),
                    PermisoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolPermisos", x => new { x.RolId, x.PermisoId });
                    table.ForeignKey(
                        name: "FK_RolPermisos_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalTable: "Permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolPermisos_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Espacios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Edificio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Piso = table.Column<int>(type: "integer", nullable: false),
                    Sector = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CapacidadMaxima = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    DepartamentoId = table.Column<int>(type: "integer", nullable: false),
                    TipoEspacioId = table.Column<int>(type: "integer", nullable: false),
                    FechaBaja = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Espacios", x => x.Id);
                    table.CheckConstraint("CK_Espacio_CapacidadPositiva", "\"CapacidadMaxima\" > 0");
                    table.ForeignKey(
                        name: "FK_Espacios_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Espacios_TiposEspacio_TipoEspacioId",
                        column: x => x.TipoEspacioId,
                        principalTable: "TiposEspacio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Auditorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    FechaHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Accion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Entidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntidadId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Detalle = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Auditorias_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Mensaje = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Leida = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioRoles",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    RolId = table.Column<int>(type: "integer", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRoles", x => new { x.UsuarioId, x.RolId });
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cursadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MateriaId = table.Column<int>(type: "integer", nullable: false),
                    CicloLectivoId = table.Column<int>(type: "integer", nullable: false),
                    Periodo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cursadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cursadas_CiclosLectivos_CicloLectivoId",
                        column: x => x.CicloLectivoId,
                        principalTable: "CiclosLectivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cursadas_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bloqueos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EspacioId = table.Column<int>(type: "integer", nullable: false),
                    FechaHoraDesde = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHoraHasta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bloqueos", x => x.Id);
                    table.CheckConstraint("CK_Bloqueo_HastaMayorDesde", "\"FechaHoraHasta\" > \"FechaHoraDesde\"");
                    table.ForeignKey(
                        name: "FK_Bloqueos_Espacios_EspacioId",
                        column: x => x.EspacioId,
                        principalTable: "Espacios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bloqueos_Usuarios_CreadoPorUsuarioId",
                        column: x => x.CreadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EspacioEquipamientos",
                columns: table => new
                {
                    EspacioId = table.Column<int>(type: "integer", nullable: false),
                    EquipamientoId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ValorOpcional = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EspacioEquipamientos", x => new { x.EspacioId, x.EquipamientoId });
                    table.CheckConstraint("CK_EspacioEquipamiento_CantidadPositiva", "\"Cantidad\" > 0");
                    table.ForeignKey(
                        name: "FK_EspacioEquipamientos_Equipamientos_EquipamientoId",
                        column: x => x.EquipamientoId,
                        principalTable: "Equipamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EspacioEquipamientos_Espacios_EspacioId",
                        column: x => x.EspacioId,
                        principalTable: "Espacios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SolicitanteId = table.Column<int>(type: "integer", nullable: false),
                    EspacioId = table.Column<int>(type: "integer", nullable: false),
                    FechaHoraInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHoraFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TipoActividad = table.Column<int>(type: "integer", nullable: false),
                    AsistentesEstimados = table.Column<int>(type: "integer", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    ResueltoPorUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    FechaResolucion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaSolicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.CheckConstraint("CK_Reserva_AsistentesPositivos", "\"AsistentesEstimados\" > 0");
                    table.CheckConstraint("CK_Reserva_FinMayorInicio", "\"FechaHoraFin\" > \"FechaHoraInicio\"");
                    table.ForeignKey(
                        name: "FK_Reservas_Espacios_EspacioId",
                        column: x => x.EspacioId,
                        principalTable: "Espacios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Usuarios_ResueltoPorUsuarioId",
                        column: x => x.ResueltoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Usuarios_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Comisiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CursadaId = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Turno = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CantidadInscriptos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comisiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comisiones_Cursadas_CursadaId",
                        column: x => x.CursadaId,
                        principalTable: "Cursadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReservaEquipamientos",
                columns: table => new
                {
                    ReservaId = table.Column<int>(type: "integer", nullable: false),
                    EquipamientoId = table.Column<int>(type: "integer", nullable: false),
                    CantidadRequerida = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Observaciones = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservaEquipamientos", x => new { x.ReservaId, x.EquipamientoId });
                    table.CheckConstraint("CK_ReservaEquipamiento_CantidadPositiva", "\"CantidadRequerida\" > 0");
                    table.ForeignKey(
                        name: "FK_ReservaEquipamientos_Equipamientos_EquipamientoId",
                        column: x => x.EquipamientoId,
                        principalTable: "Equipamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservaEquipamientos_Reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "Reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AsignacionesEspacio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComisionId = table.Column<int>(type: "integer", nullable: false),
                    EspacioId = table.Column<int>(type: "integer", nullable: false),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "interval", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesEspacio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsignacionesEspacio_Comisiones_ComisionId",
                        column: x => x.ComisionId,
                        principalTable: "Comisiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsignacionesEspacio_Espacios_EspacioId",
                        column: x => x.EspacioId,
                        principalTable: "Espacios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialesMatricula",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComisionId = table.Column<int>(type: "integer", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CantidadAlumnosPresentes = table.Column<int>(type: "integer", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialesMatricula", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialesMatricula_Comisiones_ComisionId",
                        column: x => x.ComisionId,
                        principalTable: "Comisiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesEspacio_ComisionId",
                table: "AsignacionesEspacio",
                column: "ComisionId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesEspacio_EspacioId_DiaSemana_HoraInicio_HoraFin",
                table: "AsignacionesEspacio",
                columns: new[] { "EspacioId", "DiaSemana", "HoraInicio", "HoraFin" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Entidad_EntidadId",
                table: "Auditorias",
                columns: new[] { "Entidad", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_FechaHora",
                table: "Auditorias",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_UsuarioId",
                table: "Auditorias",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Bloqueos_CreadoPorUsuarioId",
                table: "Bloqueos",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Bloqueos_EspacioId_FechaHoraDesde_FechaHoraHasta",
                table: "Bloqueos",
                columns: new[] { "EspacioId", "FechaHoraDesde", "FechaHoraHasta" });

            migrationBuilder.CreateIndex(
                name: "IX_CiclosLectivos_Anio",
                table: "CiclosLectivos",
                column: "Anio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comisiones_CursadaId",
                table: "Comisiones",
                column: "CursadaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cursadas_CicloLectivoId",
                table: "Cursadas",
                column: "CicloLectivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Cursadas_MateriaId",
                table: "Cursadas",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Departamentos_Nombre",
                table: "Departamentos",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipamientos_Nombre",
                table: "Equipamientos",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EspacioEquipamientos_EquipamientoId",
                table: "EspacioEquipamientos",
                column: "EquipamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_Espacios_Codigo",
                table: "Espacios",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Espacios_DepartamentoId",
                table: "Espacios",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Espacios_TipoEspacioId",
                table: "Espacios",
                column: "TipoEspacioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesMatricula_ComisionId",
                table: "HistorialesMatricula",
                column: "ComisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Materias_Codigo",
                table: "Materias",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materias_DepartamentoId",
                table: "Materias",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_UsuarioId",
                table: "Notificaciones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_Codigo",
                table: "Permisos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservaEquipamientos_EquipamientoId",
                table: "ReservaEquipamientos",
                column: "EquipamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_EspacioId_FechaHoraInicio_FechaHoraFin",
                table: "Reservas",
                columns: new[] { "EspacioId", "FechaHoraInicio", "FechaHoraFin" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_ResueltoPorUsuarioId",
                table: "Reservas",
                column: "ResueltoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_SolicitanteId",
                table: "Reservas",
                column: "SolicitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Nombre",
                table: "Roles",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_PermisoId",
                table: "RolPermisos",
                column: "PermisoId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposEspacio_Nombre",
                table: "TiposEspacio",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_RolId",
                table: "UsuarioRoles",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionesEspacio");

            migrationBuilder.DropTable(
                name: "Auditorias");

            migrationBuilder.DropTable(
                name: "Bloqueos");

            migrationBuilder.DropTable(
                name: "EspacioEquipamientos");

            migrationBuilder.DropTable(
                name: "HistorialesMatricula");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "ReservaEquipamientos");

            migrationBuilder.DropTable(
                name: "RolPermisos");

            migrationBuilder.DropTable(
                name: "UsuarioRoles");

            migrationBuilder.DropTable(
                name: "Comisiones");

            migrationBuilder.DropTable(
                name: "Equipamientos");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Cursadas");

            migrationBuilder.DropTable(
                name: "Espacios");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "CiclosLectivos");

            migrationBuilder.DropTable(
                name: "Materias");

            migrationBuilder.DropTable(
                name: "TiposEspacio");

            migrationBuilder.DropTable(
                name: "Departamentos");
        }
    }
}
