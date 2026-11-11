using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionReservas.Infrastructure.Persistence;

/// <summary>
/// Contexto principal de Entity Framework Core para PostgreSQL.
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<TipoEspacio> TiposEspacio => Set<TipoEspacio>();
    public DbSet<Espacio> Espacios => Set<Espacio>();
    public DbSet<Equipamiento> Equipamientos => Set<Equipamiento>();
    public DbSet<EspacioEquipamiento> EspaciosEquipamiento => Set<EspacioEquipamiento>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<ReservaEquipamiento> ReservasEquipamiento => Set<ReservaEquipamiento>();
    public DbSet<Bloqueo> Bloqueos => Set<Bloqueo>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<CicloLectivo> CiclosLectivos => Set<CicloLectivo>();
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<Cursada> Cursadas => Set<Cursada>();
    public DbSet<Comision> Comisiones => Set<Comision>();
    public DbSet<AsignacionEspacio> AsignacionesEspacio => Set<AsignacionEspacio>();
    public DbSet<HistorialMatricula> HistorialesMatricula => Set<HistorialMatricula>();

    public async Task AcquireAdvisoryXactLockAsync(long key, CancellationToken cancellationToken = default)
    {
        await Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas las configuraciones IEntityTypeConfiguration del ensamblado de Infrastructure
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

