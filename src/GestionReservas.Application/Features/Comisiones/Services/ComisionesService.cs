using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Features.Comisiones.DTOs;
using GestionReservas.Application.Features.Espacios;
using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionReservas.Application.Features.Comisiones.Services;

public sealed class ComisionesService : IComisionesService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IEspacioAuthorizationService _authorization;

    public ComisionesService(IApplicationDbContext context, ICurrentUserService currentUser, IEspacioAuthorizationService authorization)
    {
        _context = context;
        _currentUser = currentUser;
        _authorization = authorization;
    }

    public async Task<IReadOnlyList<ComisionListadoDto>> ListarAsync(int departamentoId, CancellationToken cancellationToken = default)
    {
        await _authorization.ValidarPuedeAdministrarDepartamentoAsync(departamentoId, cancellationToken);
        return await _context.Comisiones.AsNoTracking()
            .Where(c => c.Cursada.Materia.DepartamentoId == departamentoId)
            .OrderBy(c => c.Cursada.CicloLectivo.Anio).ThenBy(c => c.Cursada.Materia.Codigo).ThenBy(c => c.Codigo)
            .Select(c => new ComisionListadoDto(c.Id, c.Codigo, c.Cursada.Materia.Codigo, c.Cursada.Materia.Nombre,
                c.Cursada.Materia.DepartamentoId, c.Cursada.Materia.Departamento.Nombre, c.Cursada.CicloLectivo.Anio,
                c.Cursada.Periodo, c.Turno, c.Modalidad, c.CantidadInscriptos,
                c.Asignaciones.Any() ? "Asignada" : "SinAsignar",
                c.Asignaciones.OrderBy(a => a.DiaSemana).ThenBy(a => a.HoraInicio)
                    .Select(a => new AsignacionComisionDto(a.Id, a.EspacioId, a.Espacio.Codigo, a.Espacio.Nombre,
                        a.DiaSemana, a.HoraInicio, a.HoraFin)).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<AsignacionComisionDto> AsignarAsync(int comisionId, GuardarAsignacionComisionRequest request, CancellationToken cancellationToken = default)
    {
        var comision = await ObtenerComisionAutorizadaAsync(comisionId, cancellationToken);
        ValidarHorario(request);
        var espacio = await ObtenerEspacioAutorizadoAsync(request.EspacioId, cancellationToken);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _context.AcquireAdvisoryXactLockAsync(espacio.Id, cancellationToken);
        await ValidarConflictosAsync(comision, espacio.Id, request, null, cancellationToken);

        var asignacion = new AsignacionEspacio
        {
            ComisionId = comisionId, EspacioId = espacio.Id, DiaSemana = request.DiaSemana,
            HoraInicio = request.HoraInicio, HoraFin = request.HoraFin
        };
        _context.AsignacionesEspacio.Add(asignacion);
        await _context.SaveChangesAsync(cancellationToken);
        await AuditarAsync("AsignarEspacioComision", asignacion.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(asignacion, espacio.Codigo, espacio.Nombre);
    }

    public async Task<AsignacionComisionDto> ReasignarAsync(int comisionId, int asignacionId, GuardarAsignacionComisionRequest request, CancellationToken cancellationToken = default)
    {
        var comision = await ObtenerComisionAutorizadaAsync(comisionId, cancellationToken);
        var asignacion = await _context.AsignacionesEspacio.FirstOrDefaultAsync(a => a.Id == asignacionId && a.ComisionId == comisionId, cancellationToken)
            ?? throw new NotFoundException("AsignacionEspacio", asignacionId);
        var espacioAnterior = await _context.Espacios.AsNoTracking().FirstAsync(e => e.Id == asignacion.EspacioId, cancellationToken);
        await _authorization.ValidarPuedeAdministrarEspacioAsync(espacioAnterior, cancellationToken);
        ValidarHorario(request);
        var espacio = await ObtenerEspacioAutorizadoAsync(request.EspacioId, cancellationToken);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        foreach (var espacioLockId in new[] { asignacion.EspacioId, espacio.Id }.Distinct().OrderBy(x => x))
            await _context.AcquireAdvisoryXactLockAsync(espacioLockId, cancellationToken);
        await ValidarConflictosAsync(comision, espacio.Id, request, asignacionId, cancellationToken);
        asignacion.EspacioId = espacio.Id;
        asignacion.DiaSemana = request.DiaSemana;
        asignacion.HoraInicio = request.HoraInicio;
        asignacion.HoraFin = request.HoraFin;
        await _context.SaveChangesAsync(cancellationToken);
        await AuditarAsync("ReasignarEspacioComision", asignacion.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(asignacion, espacio.Codigo, espacio.Nombre);
    }

    public async Task QuitarAsync(int comisionId, int asignacionId, CancellationToken cancellationToken = default)
    {
        await ObtenerComisionAutorizadaAsync(comisionId, cancellationToken);
        var asignacion = await _context.AsignacionesEspacio.Include(a => a.Espacio)
            .FirstOrDefaultAsync(a => a.Id == asignacionId && a.ComisionId == comisionId, cancellationToken)
            ?? throw new NotFoundException("AsignacionEspacio", asignacionId);
        await _authorization.ValidarPuedeAdministrarEspacioAsync(asignacion.Espacio, cancellationToken);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _context.AcquireAdvisoryXactLockAsync(asignacion.EspacioId, cancellationToken);
        _context.AsignacionesEspacio.Remove(asignacion);
        await _context.SaveChangesAsync(cancellationToken);
        await AuditarAsync("QuitarEspacioComision", asignacionId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<Comision> ObtenerComisionAutorizadaAsync(int id, CancellationToken ct)
    {
        var comision = await _context.Comisiones.AsNoTracking().Include(c => c.Cursada).ThenInclude(c => c.Materia)
            .FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException("Comision", id);
        await _authorization.ValidarPuedeAdministrarDepartamentoAsync(comision.Cursada.Materia.DepartamentoId, ct);
        return comision;
    }

    private async Task<Espacio> ObtenerEspacioAutorizadoAsync(int id, CancellationToken ct)
    {
        var espacio = await _context.Espacios.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.FechaBaja == null, ct)
            ?? throw new NotFoundException("Espacio", id);
        await _authorization.ValidarPuedeAdministrarEspacioAsync(espacio, ct);
        return espacio;
    }

    private async Task ValidarConflictosAsync(Comision comision, int espacioId, GuardarAsignacionComisionRequest request, int? excluirId, CancellationToken ct)
    {
        var horaSuperpuesta = await _context.AsignacionesEspacio.AsNoTracking().AnyAsync(a =>
            a.Id != excluirId && a.EspacioId == espacioId && a.DiaSemana == request.DiaSemana &&
            a.Comision.Cursada.CicloLectivo.Activo &&
            a.Comision.Cursada.CicloLectivo.FechaInicio <= comision.Cursada.CicloLectivo.FechaFin &&
            a.Comision.Cursada.CicloLectivo.FechaFin >= comision.Cursada.CicloLectivo.FechaInicio &&
            a.HoraInicio < request.HoraFin && a.HoraFin > request.HoraInicio, ct);
        if (horaSuperpuesta) throw new ConflictException("El espacio ya tiene una comisión asignada en ese día y horario.");

        var espacioInhabilitado = await _context.Espacios.AnyAsync(e => e.Id == espacioId && e.Estado != GestionReservas.Domain.Enums.EstadoEspacio.Disponible, ct);
        if (espacioInhabilitado) throw new ConflictException("No se puede asignar un espacio que no está disponible.");

        var ciclo = comision.Cursada.CicloLectivo;
        var fechas = Enumerable.Range(0, (ciclo.FechaFin.Date - ciclo.FechaInicio.Date).Days + 1)
            .Select(offset => ciclo.FechaInicio.Date.AddDays(offset)).Where(d => d.DayOfWeek == request.DiaSemana).ToArray();
        if (fechas.Length == 0) return;
        var primerInicio = DateTimeZoneHelper.NormalizeToUtc(DateTime.SpecifyKind(fechas[0].Add(request.HoraInicio), DateTimeKind.Unspecified));
        var ultimoFin = DateTimeZoneHelper.NormalizeToUtc(DateTime.SpecifyKind(fechas[^1].Add(request.HoraFin), DateTimeKind.Unspecified));
        var reservas = await _context.Reservas.AsNoTracking().Where(r => r.EspacioId == espacioId &&
            r.Estado == GestionReservas.Domain.Enums.EstadoReserva.Aprobada && r.FechaHoraInicio < ultimoFin && r.FechaHoraFin > primerInicio)
            .Select(r => new { r.FechaHoraInicio, r.FechaHoraFin }).ToListAsync(ct);
        var bloqueos = await _context.Bloqueos.AsNoTracking().Where(b => b.EspacioId == espacioId &&
            b.FechaHoraDesde < ultimoFin && b.FechaHoraHasta > primerInicio)
            .Select(b => new { b.FechaHoraDesde, b.FechaHoraHasta }).ToListAsync(ct);
        foreach (var fecha in fechas)
        {
            var inicio = DateTimeZoneHelper.NormalizeToUtc(DateTime.SpecifyKind(fecha.Add(request.HoraInicio), DateTimeKind.Unspecified));
            var fin = DateTimeZoneHelper.NormalizeToUtc(DateTime.SpecifyKind(fecha.Add(request.HoraFin), DateTimeKind.Unspecified));
            var conflicto = reservas.Any(r => r.FechaHoraInicio < fin && r.FechaHoraFin > inicio) ||
                bloqueos.Any(b => b.FechaHoraDesde < fin && b.FechaHoraHasta > inicio);
            if (conflicto) throw new ConflictException($"El espacio tiene una reserva o bloqueo incompatible el {fecha:yyyy-MM-dd}.");
        }
    }

    private static void ValidarHorario(GuardarAsignacionComisionRequest request)
    {
        if (!Enum.IsDefined(request.DiaSemana) || request.HoraInicio < TimeSpan.Zero || request.HoraFin > TimeSpan.FromDays(1) || request.HoraFin <= request.HoraInicio)
            throw new GestionReservas.Application.Common.Exceptions.ValidationException("Horario", "El día u horario de la asignación no es válido.");
    }

    private async Task AuditarAsync(string accion, int asignacionId, CancellationToken ct)
    {
        _context.Auditorias.Add(new Auditoria { UsuarioId = _currentUser.UsuarioId, FechaHora = DateTime.UtcNow,
            Accion = accion, Entidad = "AsignacionEspacio", EntidadId = asignacionId.ToString(),
            Motivo = "Gestión de asignación académica", Detalle = $"{accion} para asignación #{asignacionId}." });
        await _context.SaveChangesAsync(ct);
    }

    private static AsignacionComisionDto Map(AsignacionEspacio a, string codigo, string nombre) =>
        new(a.Id, a.EspacioId, codigo, nombre, a.DiaSemana, a.HoraInicio, a.HoraFin);
}
