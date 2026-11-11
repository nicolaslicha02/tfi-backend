using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionReservas.Application.Common.Interfaces;

/// <summary>
/// Contrato para el contexto de persistencia accesible desde la capa de aplicación.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Rol> Roles { get; }
    DbSet<Permiso> Permisos { get; }
    DbSet<UsuarioRol> UsuarioRoles { get; }
    DbSet<RolPermiso> RolPermisos { get; }
    DbSet<Departamento> Departamentos { get; }
    DbSet<TipoEspacio> TiposEspacio { get; }
    DbSet<Espacio> Espacios { get; }
    DbSet<Equipamiento> Equipamientos { get; }
    DbSet<EspacioEquipamiento> EspaciosEquipamiento { get; }
    DbSet<Reserva> Reservas { get; }
    DbSet<ReservaEquipamiento> ReservasEquipamiento { get; }
    DbSet<Bloqueo> Bloqueos { get; }
    DbSet<Notificacion> Notificaciones { get; }
    DbSet<Auditoria> Auditorias { get; }
    DbSet<CicloLectivo> CiclosLectivos { get; }
    DbSet<Materia> Materias { get; }
    DbSet<Cursada> Cursadas { get; }
    DbSet<Comision> Comisiones { get; }
    DbSet<AsignacionEspacio> AsignacionesEspacio { get; }
    DbSet<HistorialMatricula> HistorialesMatricula { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
