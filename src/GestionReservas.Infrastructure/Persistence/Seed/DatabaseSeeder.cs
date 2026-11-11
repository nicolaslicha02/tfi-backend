using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Domain.Entities;
using GestionReservas.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestionReservas.Infrastructure.Persistence.Seed;

/// <summary>
/// Sembrador de datos iniciales para la base de datos (roles, permisos, departamentos, tipos de espacio,
/// equipamientos, espacios físicos y usuarios de prueba).
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            if (await _context.Roles.AnyAsync())
            {
                _logger.LogInformation("La base de datos ya contiene datos iniciales. Se omite el seed.");
                return;
            }

            _logger.LogInformation("Iniciando sembrado de datos iniciales...");

            // 1. Roles
            var rolSolicitante = new Rol
            {
                Nombre = "Solicitante",
                Descripcion = "Docente o investigador que puede solicitar reservas de espacios y consultar el estado de sus pedidos."
            };

            var rolResponsable = new Rol
            {
                Nombre = "ResponsableEspacio",
                Descripcion = "Responsable de departamento encargado de revisar, aprobar o rechazar solicitudes de reserva en sus espacios."
            };

            var rolSecretaria = new Rol
            {
                Nombre = "SecretariaAcademica",
                Descripcion = "Gestión de la ocupación académica oficial recurrente, comisiones y bloqueos institucionales de espacios."
            };

            var rolAdmin = new Rol
            {
                Nombre = "Administrador",
                Descripcion = "Administrador general del sistema con acceso total a usuarios, espacios, catálogos y bitácora de auditoría."
            };

            _context.Roles.AddRange(rolSolicitante, rolResponsable, rolSecretaria, rolAdmin);
            await _context.SaveChangesAsync();

            // 2. Permisos básicos
            var permisos = new List<Permiso>
            {
                new() { Codigo = "espacios.ver", Descripcion = "Ver listado y detalle de espacios" },
                new() { Codigo = "espacios.crear", Descripcion = "Crear nuevos espacios físicos" },
                new() { Codigo = "espacios.editar", Descripcion = "Modificar configuración de espacios" },
                new() { Codigo = "espacios.eliminar", Descripcion = "Dar de baja lógica a espacios" },

                new() { Codigo = "reservas.solicitar", Descripcion = "Crear solicitudes de reserva de espacio" },
                new() { Codigo = "reservas.ver_propias", Descripcion = "Consultar el historial de reservas propias" },
                new() { Codigo = "reservas.ver_pendientes", Descripcion = "Ver solicitudes pendientes de resolución" },
                new() { Codigo = "reservas.aprobar_rechazar", Descripcion = "Aprobar o rechazar solicitudes de reserva" },
                new() { Codigo = "reservas.cancelar", Descripcion = "Cancelar solicitudes de reserva activas" },

                new() { Codigo = "bloqueos.ver", Descripcion = "Listar bloqueos de espacios" },
                new() { Codigo = "bloqueos.crear", Descripcion = "Crear bloqueos por mantenimiento o reformas" },

                new() { Codigo = "catalogos.ver", Descripcion = "Consultar catálogos de departamentos y equipamientos" },
                new() { Codigo = "auditoria.ver", Descripcion = "Consultar bitácora institucional de auditoría" },
                new() { Codigo = "usuarios.gestionar", Descripcion = "Administrar cuentas y roles de usuarios" }
            };

            _context.Permisos.AddRange(permisos);
            await _context.SaveChangesAsync();

            // Asignación de permisos a roles
            // Solicitante:
            AsignarPermisos(rolSolicitante, permisos, "espacios.ver", "reservas.solicitar", "reservas.ver_propias", "reservas.cancelar", "catalogos.ver");

            // ResponsableEspacio:
            AsignarPermisos(rolResponsable, permisos, "espacios.ver", "reservas.solicitar", "reservas.ver_propias", "reservas.ver_pendientes", "reservas.aprobar_rechazar", "reservas.cancelar", "bloqueos.ver", "bloqueos.crear", "catalogos.ver");

            // SecretariaAcademica:
            AsignarPermisos(rolSecretaria, permisos, "espacios.ver", "reservas.ver_pendientes", "reservas.aprobar_rechazar", "reservas.cancelar", "bloqueos.ver", "bloqueos.crear", "catalogos.ver");

            // Administrador: todos los permisos
            foreach (var permiso in permisos)
            {
                _context.RolPermisos.Add(new RolPermiso { RolId = rolAdmin.Id, PermisoId = permiso.Id });
            }
            await _context.SaveChangesAsync();

            // 3. Departamentos (3 solicitados)
            var deptoSistemas = new Departamento
            {
                Nombre = "Departamento de Sistemas e Informática",
                Descripcion = "Gestión de laboratorios de computación, redes e infraestructura informática."
            };

            var deptoBasicas = new Departamento
            {
                Nombre = "Departamento de Ciencias Básicas",
                Descripcion = "Administración de aulas teóricas de matemática, física y química."
            };

            var deptoSecretaria = new Departamento
            {
                Nombre = "Secretaría Académica",
                Descripcion = "Gobierno académico, aulas magnas, auditorios y salas institucionales."
            };

            _context.Departamentos.AddRange(deptoSistemas, deptoBasicas, deptoSecretaria);
            await _context.SaveChangesAsync();

            // 4. Tipos de Espacio
            var tipoAuditorio = new TipoEspacio { Nombre = "Auditorio", Descripcion = "Espacio de gran capacidad para conferencias y actos" };
            var tipoAulaTeorica = new TipoEspacio { Nombre = "Aula Teórica", Descripcion = "Aula equipada con pupitres y pizarra para clases expositivas" };
            var tipoLaboratorio = new TipoEspacio { Nombre = "Laboratorio de Computación", Descripcion = "Espacio con computadoras para prácticas individuales" };
            var tipoSalaReuniones = new TipoEspacio { Nombre = "Sala de Reuniones", Descripcion = "Espacio para reuniones directivas, cátedras o posgrados" };

            _context.TiposEspacio.AddRange(tipoAuditorio, tipoAulaTeorica, tipoLaboratorio, tipoSalaReuniones);
            await _context.SaveChangesAsync();

            // 5. Equipamientos (8 equipamientos solicitados en catálogo flexible)
            var eq1 = new Equipamiento { Nombre = "Proyector Láser Full HD", Descripcion = "Proyector de alta definición montado en techo" };
            var eq2 = new Equipamiento { Nombre = "Pantalla Retráctil Motorizada", Descripcion = "Pantalla de proyección retráctil con control remoto" };
            var eq3 = new Equipamiento { Nombre = "Sistema de Sonido y Micrófono", Descripcion = "Amplificador con parlantes de techo y micrófono inalámbrico" };
            var eq4 = new Equipamiento { Nombre = "Pizarra Digital Interactiva", Descripcion = "Pantalla interactiva multitáctil con soporte USB" };
            var eq5 = new Equipamiento { Nombre = "PC Docente con Conectividad HDMI", Descripcion = "Computadora de escritorio dedicada al expositor" };
            var eq6 = new Equipamiento { Nombre = "Equipos de Computación Alumnos (30 PCs)", Descripcion = "Puestos de trabajo informáticos para prácticas estudiantiles" };
            var eq7 = new Equipamiento { Nombre = "Aire Acondicionado Central", Descripcion = "Equipo de climatización frío/calor con control termostático" };
            var eq8 = new Equipamiento { Nombre = "Cámara para Transmisión Híbrida / Streaming", Descripcion = "Cámara PTZ con seguimiento automático para clases virtuales" };

            _context.Equipamientos.AddRange(eq1, eq2, eq3, eq4, eq5, eq6, eq7, eq8);
            await _context.SaveChangesAsync();

            // 6. Espacios de ejemplo (6 solicitados)
            var espacios = new List<Espacio>
            {
                new()
                {
                    Codigo = "AULA-101",
                    Nombre = "Aula Magna 101",
                    Edificio = "Edificio Central",
                    Piso = 1,
                    Sector = "Ala Norte",
                    CapacidadMaxima = 120,
                    Estado = EstadoEspacio.Disponible,
                    DepartamentoId = deptoSecretaria.Id,
                    TipoEspacioId = tipoAuditorio.Id
                },
                new()
                {
                    Codigo = "AULA-201",
                    Nombre = "Aula Teórica 201",
                    Edificio = "Edificio Central",
                    Piso = 2,
                    Sector = "Ala Sur",
                    CapacidadMaxima = 60,
                    Estado = EstadoEspacio.Disponible,
                    DepartamentoId = deptoBasicas.Id,
                    TipoEspacioId = tipoAulaTeorica.Id
                },
                new()
                {
                    Codigo = "AULA-202",
                    Nombre = "Aula Teórica 202",
                    Edificio = "Edificio Central",
                    Piso = 2,
                    Sector = "Ala Sur",
                    CapacidadMaxima = 45,
                    Estado = EstadoEspacio.Disponible,
                    DepartamentoId = deptoBasicas.Id,
                    TipoEspacioId = tipoAulaTeorica.Id
                },
                new()
                {
                    Codigo = "LAB-SIS-01",
                    Nombre = "Laboratorio de Redes y Software",
                    Edificio = "Edificio de Tecnologías",
                    Piso = 1,
                    Sector = "Ala Este",
                    CapacidadMaxima = 32,
                    Estado = EstadoEspacio.Disponible,
                    DepartamentoId = deptoSistemas.Id,
                    TipoEspacioId = tipoLaboratorio.Id
                },
                new()
                {
                    Codigo = "LAB-SIS-02",
                    Nombre = "Laboratorio de Inteligencia Artificial",
                    Edificio = "Edificio de Tecnologías",
                    Piso = 1,
                    Sector = "Ala Este",
                    CapacidadMaxima = 30,
                    Estado = EstadoEspacio.Disponible,
                    DepartamentoId = deptoSistemas.Id,
                    TipoEspacioId = tipoLaboratorio.Id
                },
                new()
                {
                    Codigo = "SALA-POS-01",
                    Nombre = "Sala de Consejo y Posgrado",
                    Edificio = "Edificio de Gobierno",
                    Piso = 3,
                    Sector = "Decanato",
                    CapacidadMaxima = 25,
                    Estado = EstadoEspacio.Disponible,
                    DepartamentoId = deptoSecretaria.Id,
                    TipoEspacioId = tipoSalaReuniones.Id
                }
            };

            _context.Espacios.AddRange(espacios);
            await _context.SaveChangesAsync();

            // Asociación de equipamiento a espacios vía tabla intermedia EspacioEquipamiento
            // AULA-101 (Aula Magna): Proyector, Pantalla retráctil, Sonido, Aire, Cámara Streaming
            _context.EspaciosEquipamiento.AddRange(
                new EspacioEquipamiento { EspacioId = espacios[0].Id, EquipamientoId = eq1.Id, Cantidad = 1, ValorOpcional = "4K Láser", Observaciones = "Control remoto en estrado" },
                new EspacioEquipamiento { EspacioId = espacios[0].Id, EquipamientoId = eq2.Id, Cantidad = 1, ValorOpcional = "150 pulgadas" },
                new EspacioEquipamiento { EspacioId = espacios[0].Id, EquipamientoId = eq3.Id, Cantidad = 1, ValorOpcional = "2 micrófonos inalámbricos" },
                new EspacioEquipamiento { EspacioId = espacios[0].Id, EquipamientoId = eq7.Id, Cantidad = 2, ValorOpcional = "6000 frigorías" },
                new EspacioEquipamiento { EspacioId = espacios[0].Id, EquipamientoId = eq8.Id, Cantidad = 1, ValorOpcional = "Cámara PTZ USB/HDMI" }
            );

            // AULA-201: Proyector, PC Docente, Aire
            _context.EspaciosEquipamiento.AddRange(
                new EspacioEquipamiento { EspacioId = espacios[1].Id, EquipamientoId = eq1.Id, Cantidad = 1, ValorOpcional = "Full HD" },
                new EspacioEquipamiento { EspacioId = espacios[1].Id, EquipamientoId = eq5.Id, Cantidad = 1, ValorOpcional = "Intel Core i5" },
                new EspacioEquipamiento { EspacioId = espacios[1].Id, EquipamientoId = eq7.Id, Cantidad = 1, ValorOpcional = "4500 frigorías" }
            );

            // AULA-202: Proyector, PC Docente
            _context.EspaciosEquipamiento.AddRange(
                new EspacioEquipamiento { EspacioId = espacios[2].Id, EquipamientoId = eq1.Id, Cantidad = 1, ValorOpcional = "Full HD" },
                new EspacioEquipamiento { EspacioId = espacios[2].Id, EquipamientoId = eq5.Id, Cantidad = 1, ValorOpcional = "Intel Core i5" }
            );

            // LAB-SIS-01: Proyector, PC Docente, 30 PCs alumnos, Aire
            _context.EspaciosEquipamiento.AddRange(
                new EspacioEquipamiento { EspacioId = espacios[3].Id, EquipamientoId = eq1.Id, Cantidad = 1, ValorOpcional = "Full HD" },
                new EspacioEquipamiento { EspacioId = espacios[3].Id, EquipamientoId = eq5.Id, Cantidad = 1, ValorOpcional = "PC Docente i7" },
                new EspacioEquipamiento { EspacioId = espacios[3].Id, EquipamientoId = eq6.Id, Cantidad = 30, ValorOpcional = "30 PCs Core i5 16GB" },
                new EspacioEquipamiento { EspacioId = espacios[3].Id, EquipamientoId = eq7.Id, Cantidad = 2, ValorOpcional = "5000 frigorías" }
            );

            // LAB-SIS-02: Pizarra digital, PC Docente, 30 PCs alumnos, Aire
            _context.EspaciosEquipamiento.AddRange(
                new EspacioEquipamiento { EspacioId = espacios[4].Id, EquipamientoId = eq4.Id, Cantidad = 1, ValorOpcional = "Pantalla táctil 75''" },
                new EspacioEquipamiento { EspacioId = espacios[4].Id, EquipamientoId = eq5.Id, Cantidad = 1, ValorOpcional = "PC Docente i7 GPU dedicada" },
                new EspacioEquipamiento { EspacioId = espacios[4].Id, EquipamientoId = eq6.Id, Cantidad = 30, ValorOpcional = "30 PCs Core i7 GPU Nvidia" },
                new EspacioEquipamiento { EspacioId = espacios[4].Id, EquipamientoId = eq7.Id, Cantidad = 2, ValorOpcional = "5000 frigorías" }
            );

            // SALA-POS-01: Pizarra digital, Sonido, Cámara streaming, Aire
            _context.EspaciosEquipamiento.AddRange(
                new EspacioEquipamiento { EspacioId = espacios[5].Id, EquipamientoId = eq4.Id, Cantidad = 1, ValorOpcional = "Pantalla 65''" },
                new EspacioEquipamiento { EspacioId = espacios[5].Id, EquipamientoId = eq3.Id, Cantidad = 1, ValorOpcional = "Micrófono omnidireccional mesa redonda" },
                new EspacioEquipamiento { EspacioId = espacios[5].Id, EquipamientoId = eq8.Id, Cantidad = 1, ValorOpcional = "Sistema Teams/Zoom Rooms" },
                new EspacioEquipamiento { EspacioId = espacios[5].Id, EquipamientoId = eq7.Id, Cantidad = 1, ValorOpcional = "3000 frigorías" }
            );

            await _context.SaveChangesAsync();

            // 7. Usuarios de prueba (1 por rol con contraseñas hasheadas)
            var adminUser = new Usuario
            {
                Nombre = "Administrador",
                Apellido = "General",
                NombreCompleto = "Administrador General",
                Email = "admin@facultad.edu.ar",
                PasswordHash = _passwordHasher.HashPassword("Admin123!"),
                Telefono = "+54 11 4000-0001",
                Activo = true
            };

            var secretariaUser = new Usuario
            {
                Nombre = "Secretaría",
                Apellido = "Académica",
                NombreCompleto = "Secretaría Académica",
                Email = "secretaria@facultad.edu.ar",
                PasswordHash = _passwordHasher.HashPassword("Secretaria123!"),
                Telefono = "+54 11 4000-0002",
                Activo = true
            };

            var responsableUser = new Usuario
            {
                Nombre = "Carlos",
                Apellido = "Gutiérrez",
                NombreCompleto = "Ing. Carlos Gutiérrez (Responsable Sistemas)",
                Email = "responsable.sistemas@facultad.edu.ar",
                PasswordHash = _passwordHasher.HashPassword("Responsable123!"),
                Telefono = "+54 11 4000-0003",
                Activo = true,
                DepartamentoId = deptoSistemas.Id
            };

            var solicitanteUser = new Usuario
            {
                Nombre = "Martín",
                Apellido = "Pérez",
                NombreCompleto = "Prof. Martín Pérez (Docente Solicitante)",
                Email = "docente.perez@facultad.edu.ar",
                PasswordHash = _passwordHasher.HashPassword("Docente123!"),
                Telefono = "+54 11 4000-0004",
                Activo = true,
                DepartamentoId = deptoBasicas.Id
            };

            _context.Usuarios.AddRange(adminUser, secretariaUser, responsableUser, solicitanteUser);
            await _context.SaveChangesAsync();

            // Asignación de roles a usuarios
            _context.UsuarioRoles.AddRange(
                new UsuarioRol { UsuarioId = adminUser.Id, RolId = rolAdmin.Id },
                new UsuarioRol { UsuarioId = secretariaUser.Id, RolId = rolSecretaria.Id },
                new UsuarioRol { UsuarioId = responsableUser.Id, RolId = rolResponsable.Id },
                new UsuarioRol { UsuarioId = solicitanteUser.Id, RolId = rolSolicitante.Id }
            );

            // Registro inicial en bitácora de auditoría
            _context.Auditorias.Add(new Auditoria
            {
                UsuarioId = adminUser.Id,
                FechaHora = DateTime.UtcNow,
                Accion = "SeedInicial",
                Entidad = "Sistema",
                EntidadId = "0",
                Motivo = "Inicialización automática de la base de datos con catálogo y usuarios semilla."
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Sembrado de datos iniciales completado con éxito.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrió un error al ejecutar el sembrador de base de datos.");
            throw;
        }
    }

    private void AsignarPermisos(Rol rol, IEnumerable<Permiso> catalogo, params string[] codigos)
    {
        var codigoSet = new HashSet<string>(codigos);
        foreach (var permiso in catalogo.Where(p => codigoSet.Contains(p.Codigo)))
        {
            _context.RolPermisos.Add(new RolPermiso
            {
                RolId = rol.Id,
                PermisoId = permiso.Id
            });
        }
    }
}
