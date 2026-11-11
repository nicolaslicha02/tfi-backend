using FluentValidation;
using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Espacios;
using GestionReservas.Application.Features.Notificaciones;
using GestionReservas.Application.Features.Reservas.DTOs;
using GestionReservas.Domain.Constants;
using GestionReservas.Domain.Entities;
using GestionReservas.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ValidationException = GestionReservas.Application.Common.Exceptions.ValidationException;

namespace GestionReservas.Application.Features.Reservas.Services;

/// <summary>
/// Implementación del servicio de reservas de espacios con máquina de estados, control de concurrencia y auditoría.
/// </summary>
public class ReservasService : IReservasService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDisponibilidadService _disponibilidadService;
    private readonly IEspacioAuthorizationService _espacioAuthService;
    private readonly INotificacionesService _notificacionesService;
    private readonly IValidator<CrearReservaRequestDto> _crearValidator;
    private readonly IValidator<ActualizarReservaRequestDto> _actualizarValidator;
    private readonly IValidator<CambiarEstadoReservaRequestDto> _cambiarEstadoValidator;
    private readonly IValidator<MisReservasQueryParameters> _misReservasValidator;

    public ReservasService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDisponibilidadService disponibilidadService,
        IEspacioAuthorizationService espacioAuthService,
        INotificacionesService notificacionesService,
        IValidator<CrearReservaRequestDto> crearValidator,
        IValidator<ActualizarReservaRequestDto> actualizarValidator,
        IValidator<CambiarEstadoReservaRequestDto> cambiarEstadoValidator,
        IValidator<MisReservasQueryParameters> misReservasValidator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _disponibilidadService = disponibilidadService;
        _espacioAuthService = espacioAuthService;
        _notificacionesService = notificacionesService;
        _crearValidator = crearValidator;
        _actualizarValidator = actualizarValidator;
        _cambiarEstadoValidator = cambiarEstadoValidator;
        _misReservasValidator = misReservasValidator;
    }

    public async Task<ReservaDetalleDto> CreateReservaAsync(CrearReservaRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_crearValidator, request, cancellationToken);

        var solicitanteId = _currentUserService.UsuarioId
            ?? throw new UnauthorizedException("Usuario no autenticado.");

        var inicioUtc = DateTimeZoneHelper.NormalizeToUtc(request.FechaHoraInicio);
        var finUtc = DateTimeZoneHelper.NormalizeToUtc(request.FechaHoraFin);

        var espacio = await ObtenerYValidarEspacioCapacidadYEquipamientoAsync(
            request.EspacioId,
            request.AsistentesEstimados,
            request.EquipamientoRequerido,
            cancellationToken);

        // Determinar si aplica excepción de auto-aprobación (Responsable del depto del espacio, Secretaría o Admin)
        var autoAprobar = await _espacioAuthService.PuedeAdministrarDepartamentoAsync(espacio.DepartamentoId, cancellationToken);
        var estadoInicial = ReservaStateMachine.DeterminarEstadoInicial(autoAprobar);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (autoAprobar)
            {
                // Bloqueo transaccional por espacio en PostgreSQL para evitar aprobaciones concurrentes solapadas
                await _context.AcquireAdvisoryXactLockAsync(espacio.Id, cancellationToken);
            }

            await ValidarSinBloqueosNiSuperposicionAprobadaAsync(
                espacio.Id,
                inicioUtc,
                finUtc,
                excluirReservaId: null,
                cancellationToken);

            var ahoraUtc = DateTime.UtcNow;
            var reserva = new Reserva
            {
                SolicitanteId = solicitanteId,
                EspacioId = espacio.Id,
                FechaHoraInicio = inicioUtc,
                FechaHoraFin = finUtc,
                TipoActividad = request.TipoActividad,
                AsistentesEstimados = request.AsistentesEstimados,
                Motivo = request.Motivo.Trim(),
                Observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim(),
                Estado = estadoInicial,
                FechaSolicitud = ahoraUtc,
                ResueltoPorUsuarioId = autoAprobar ? solicitanteId : null,
                FechaResolucion = autoAprobar ? ahoraUtc : null
            };

            foreach (var eq in request.EquipamientoRequerido)
            {
                reserva.EquipamientosRequeridos.Add(new ReservaEquipamiento
                {
                    EquipamientoId = eq.EquipamientoId,
                    CantidadRequerida = eq.Cantidad,
                    Observaciones = string.IsNullOrWhiteSpace(eq.Observaciones) ? null : eq.Observaciones.Trim()
                });
            }

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync(cancellationToken);

            // Registro de auditoría y notificaciones
            if (autoAprobar)
            {
                _context.Auditorias.Add(new Auditoria
                {
                    UsuarioId = solicitanteId,
                    FechaHora = ahoraUtc,
                    Accion = "CrearAutoAprobada",
                    Entidad = "Reserva",
                    EntidadId = reserva.Id.ToString(),
                    Motivo = reserva.Motivo,
                    Detalle = $"Reserva creada y auto-aprobada directamente en espacio '{espacio.Codigo}' ({inicioUtc:yyyy-MM-dd HH:mm} a {finUtc:yyyy-MM-dd HH:mm} UTC). Estado: {EstadoReserva.Aprobada}."
                });

                _notificacionesService.EncolarNotificacion(
                    solicitanteId,
                    $"Su reserva #{reserva.Id} para el espacio '{espacio.Codigo} - {espacio.Nombre}' fue creada y aprobada automáticamente.");
            }
            else
            {
                _context.Auditorias.Add(new Auditoria
                {
                    UsuarioId = solicitanteId,
                    FechaHora = ahoraUtc,
                    Accion = "SolicitarReserva",
                    Entidad = "Reserva",
                    EntidadId = reserva.Id.ToString(),
                    Motivo = reserva.Motivo,
                    Detalle = $"Solicitud de reserva creada en espacio '{espacio.Codigo}' ({inicioUtc:yyyy-MM-dd HH:mm} a {finUtc:yyyy-MM-dd HH:mm} UTC). Estado inicial: {EstadoReserva.PendienteAprobacion}."
                });

                await _notificacionesService.EncolarNotificacionResponsablesDepartamentoAsync(
                    espacio.DepartamentoId,
                    $"Nueva solicitud de reserva #{reserva.Id} pendiente de revisión para el espacio '{espacio.Codigo} - {espacio.Nombre}'.",
                    excluirUsuarioId: solicitanteId,
                    cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await ConstruirDetalleReservaAsync(reserva.Id, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException("Conflicto de concurrencia al procesar la reserva. Intente nuevamente.");
        }
        catch (DbUpdateException ex) when (EsViolacionExclusionOSolape(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException("El espacio ya posee una reserva aprobada superpuesta en ese rango horario.");
        }
    }

    public async Task<PagedResultDto<ReservaListadoDto>> GetMisReservasAsync(
        MisReservasQueryParameters queryParams,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_misReservasValidator, queryParams, cancellationToken);

        var usuarioId = _currentUserService.UsuarioId
            ?? throw new UnauthorizedException("Usuario no autenticado.");

        var query = _context.Reservas
            .AsNoTracking()
            .Where(r => r.SolicitanteId == usuarioId);

        if (queryParams.Estado.HasValue)
        {
            query = query.Where(r => r.Estado == queryParams.Estado.Value);
        }

        if (queryParams.Desde.HasValue)
        {
            var desdeUtc = DateTimeZoneHelper.NormalizeToUtc(queryParams.Desde.Value);
            query = query.Where(r => r.FechaHoraFin >= desdeUtc);
        }

        if (queryParams.Hasta.HasValue)
        {
            var hastaUtc = DateTimeZoneHelper.NormalizeToUtc(queryParams.Hasta.Value);
            query = query.Where(r => r.FechaHoraInicio <= hastaUtc);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await ProyectarListado(
                query.OrderByDescending(r => r.FechaHoraInicio).ThenByDescending(r => r.Id)
                     .Skip((queryParams.Page - 1) * queryParams.PageSize)
                     .Take(queryParams.PageSize))
            .ToListAsync(cancellationToken);

        return new PagedResultDto<ReservaListadoDto>(items, total, queryParams.Page, queryParams.PageSize);
    }

    public async Task<IReadOnlyList<ReservaListadoDto>> GetReservasPorEstadoAsync(
        EstadoReserva estado,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId
            ?? throw new UnauthorizedException("Usuario no autenticado.");

        var query = _context.Reservas
            .AsNoTracking()
            .Where(r => r.Estado == estado);

        var esAdminOSecretaria = _currentUserService.IsInRole("Administrador") ||
                                 _currentUserService.IsInRole("SecretariaAcademica");

        if (!esAdminOSecretaria)
        {
            if (!_currentUserService.IsInRole("ResponsableEspacio"))
            {
                throw new ForbiddenException("No tiene permisos para consultar las reservas de otros usuarios.");
            }

            var deptoId = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == usuarioId)
                .Select(u => u.DepartamentoId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!deptoId.HasValue)
            {
                return Array.Empty<ReservaListadoDto>();
            }

            query = query.Where(r => r.Espacio.DepartamentoId == deptoId.Value);
        }

        return await ProyectarListado(
                query.OrderBy(r => r.FechaHoraInicio).ThenBy(r => r.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<ReservaDetalleDto> GetReservaByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId
            ?? throw new UnauthorizedException("Usuario no autenticado.");

        var reservaInfo = await _context.Reservas
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new { r.Id, r.SolicitanteId, r.Espacio.DepartamentoId })
            .FirstOrDefaultAsync(cancellationToken);

        if (reservaInfo == null)
        {
            throw new NotFoundException($"No se encontró la reserva con identificador {id}.");
        }

        var esSolicitante = reservaInfo.SolicitanteId == usuarioId;
        var puedeAdministrarEspacio = await _espacioAuthService.PuedeAdministrarDepartamentoAsync(
            reservaInfo.DepartamentoId,
            cancellationToken);

        if (!esSolicitante && !puedeAdministrarEspacio)
        {
            throw new ForbiddenException("No tiene permiso para visualizar el detalle de esta reserva.");
        }

        return await ConstruirDetalleReservaAsync(id, cancellationToken);
    }

    public async Task<ReservaDetalleDto> CambiarEstadoReservaAsync(
        int id,
        CambiarEstadoReservaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_cambiarEstadoValidator, request, cancellationToken);

        var usuarioId = _currentUserService.UsuarioId
            ?? throw new UnauthorizedException("Usuario no autenticado.");

        var accionNormalizada = request.Accion.Trim().ToLowerInvariant();
        var motivoLimpio = string.IsNullOrWhiteSpace(request.Motivo) ? null : request.Motivo.Trim();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var espacioIdReserva = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => (int?)r.EspacioId)
                .FirstOrDefaultAsync(cancellationToken);

            if (espacioIdReserva == null)
            {
                throw new NotFoundException($"No se encontró la reserva con identificador {id}.");
            }

            // Lock transaccional por espacio (aprobar): serializa aprobaciones concurrentes del mismo espacio
            if (accionNormalizada == ReservaStateMachine.AccionAprobar)
            {
                await _context.AcquireAdvisoryXactLockAsync(espacioIdReserva.Value, cancellationToken);
            }

            // Se carga la reserva DESPUÉS del lock para ver el estado confirmado por transacciones previas
            var reserva = await _context.Reservas
                .Include(r => r.Espacio)
                .FirstAsync(r => r.Id == id, cancellationToken);

            // 1. Verificar permisos según la acción solicitada
            bool esIntervencionExcepcionalSecretaria = false;
            if (accionNormalizada is ReservaStateMachine.AccionAprobar or ReservaStateMachine.AccionRechazar)
            {
                if (!_currentUserService.HasPermission(PermisosDefinidos.ReservasAprobarRechazar))
                {
                    throw new ForbiddenException("No posee el permiso 'reservas.aprobar_rechazar' requerido para esta operación.");
                }

                await _espacioAuthService.ValidarPuedeAdministrarDepartamentoAsync(reserva.Espacio.DepartamentoId, cancellationToken);

                // Verificar si es Secretaría Académica interviniendo excepcionalmente en un espacio de otro departamento
                if (_currentUserService.IsInRole("SecretariaAcademica") && !_currentUserService.IsInRole("Administrador"))
                {
                    var usuarioDeptoId = await _context.Usuarios
                        .AsNoTracking()
                        .Where(u => u.Id == usuarioId)
                        .Select(u => u.DepartamentoId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (!usuarioDeptoId.HasValue || usuarioDeptoId.Value != reserva.Espacio.DepartamentoId)
                    {
                        esIntervencionExcepcionalSecretaria = true;
                        if (string.IsNullOrWhiteSpace(motivoLimpio))
                        {
                            throw new ValidationException("Motivo", "La intervención excepcional de Secretaría Académica requiere especificar un motivo obligatorio.");
                        }
                    }
                }
            }
            else if (accionNormalizada == ReservaStateMachine.AccionCancelar)
            {
                if (!_currentUserService.HasPermission(PermisosDefinidos.ReservasCancelar))
                {
                    throw new ForbiddenException("No posee el permiso 'reservas.cancelar' requerido para esta operación.");
                }

                var esDueno = reserva.SolicitanteId == usuarioId;
                var puedeAdministrarDepto = await _espacioAuthService.PuedeAdministrarDepartamentoAsync(
                    reserva.Espacio.DepartamentoId,
                    cancellationToken);

                if (!esDueno && !puedeAdministrarDepto)
                {
                    throw new ForbiddenException("Solo el solicitante de la reserva o el responsable del espacio pueden cancelarla.");
                }
            }

            // 2. Validar transición en la máquina de estados
            var estadoAnterior = reserva.Estado;
            var nuevoEstado = ReservaStateMachine.ResolverTransicion(estadoAnterior, accionNormalizada);

            // 3. Validar obligatoriedad de motivo según máquina de estados (rechazar siempre, o cancelar una Aprobada)
            if (ReservaStateMachine.RequiereMotivoObligatorio(estadoAnterior, accionNormalizada) &&
                string.IsNullOrWhiteSpace(motivoLimpio))
            {
                var accionTexto = accionNormalizada == ReservaStateMachine.AccionRechazar ? "rechazar" : "cancelar una reserva aprobada";
                throw new ValidationException("Motivo", $"Es obligatorio especificar un motivo para {accionTexto}.");
            }

            // 4. Si se aprueba, revalidar que el espacio siga disponible y sin bloqueos ni otra reserva aprobada superpuesta
            if (nuevoEstado == EstadoReserva.Aprobada)
            {
                if (reserva.Espacio.FechaBaja != null || reserva.Espacio.Estado == EstadoEspacio.FueraDeServicio)
                {
                    throw new ConflictException($"El espacio '{reserva.Espacio.Codigo}' no se encuentra disponible para aprobar reservas.");
                }

                await ValidarSinBloqueosNiSuperposicionAprobadaAsync(
                    reserva.EspacioId,
                    reserva.FechaHoraInicio,
                    reserva.FechaHoraFin,
                    excluirReservaId: reserva.Id,
                    cancellationToken);
            }

            // 5. Aplicar cambios a la reserva
            var ahoraUtc = DateTime.UtcNow;
            reserva.Estado = nuevoEstado;
            reserva.FechaResolucion = ahoraUtc;
            reserva.ResueltoPorUsuarioId = usuarioId;

            if (nuevoEstado is EstadoReserva.Rechazada or EstadoReserva.Cancelada && !string.IsNullOrWhiteSpace(motivoLimpio))
            {
                reserva.MotivoRechazo = motivoLimpio;
            }

            // 6. Registrar en Auditoría
            var accionAuditoria = nuevoEstado switch
            {
                EstadoReserva.Aprobada => esIntervencionExcepcionalSecretaria
                    ? "AprobarReserva (Intervención excepcional)"
                    : "AprobarReserva",
                EstadoReserva.Rechazada => esIntervencionExcepcionalSecretaria
                    ? "RechazarReserva (Intervención excepcional)"
                    : "RechazarReserva",
                EstadoReserva.Cancelada => "CancelarReserva",
                _ => "CambioEstadoReserva"
            };

            _context.Auditorias.Add(new Auditoria
            {
                UsuarioId = usuarioId,
                FechaHora = ahoraUtc,
                Accion = accionAuditoria,
                Entidad = "Reserva",
                EntidadId = reserva.Id.ToString(),
                Motivo = motivoLimpio,
                Detalle = esIntervencionExcepcionalSecretaria
                    ? $"Intervención excepcional de Secretaría Académica: transición de '{estadoAnterior}' a '{nuevoEstado}' en espacio '{reserva.Espacio.Codigo}'."
                    : $"Transición de estado de '{estadoAnterior}' a '{nuevoEstado}' en espacio '{reserva.Espacio.Codigo}'."
            });

            // 7. Encolar notificación para el solicitante
            var mensajeNotificacion = nuevoEstado switch
            {
                EstadoReserva.Aprobada => $"Su reserva #{reserva.Id} para el espacio '{reserva.Espacio.Codigo} - {reserva.Espacio.Nombre}' ha sido APROBADA.",
                EstadoReserva.Rechazada => $"Su reserva #{reserva.Id} para el espacio '{reserva.Espacio.Codigo} - {reserva.Espacio.Nombre}' ha sido RECHAZADA. Motivo: {motivoLimpio}",
                EstadoReserva.Cancelada => $"La reserva #{reserva.Id} para el espacio '{reserva.Espacio.Codigo} - {reserva.Espacio.Nombre}' ha sido CANCELADA.{(motivoLimpio != null ? $" Motivo: {motivoLimpio}" : string.Empty)}",
                _ => $"El estado de su reserva #{reserva.Id} cambió a {nuevoEstado}."
            };

            _notificacionesService.EncolarNotificacion(reserva.SolicitanteId, mensajeNotificacion);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await ConstruirDetalleReservaAsync(reserva.Id, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException("La reserva fue modificada simultáneamente por otro usuario. Recargue e intente nuevamente.");
        }
        catch (DbUpdateException ex) when (EsViolacionExclusionOSolape(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException("No se pudo aprobar la reserva porque otra reserva superpuesta en el mismo espacio fue aprobada simultáneamente.");
        }
    }

    public async Task<ReservaDetalleDto> UpdateReservaAsync(
        int id,
        ActualizarReservaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_actualizarValidator, request, cancellationToken);

        var usuarioId = _currentUserService.UsuarioId
            ?? throw new UnauthorizedException("Usuario no autenticado.");

        var reserva = await _context.Reservas
            .Include(r => r.Espacio)
            .Include(r => r.EquipamientosRequeridos)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (reserva == null)
        {
            throw new NotFoundException($"No se encontró la reserva con identificador {id}.");
        }

        // Verificar que quien modifica sea el dueño o pueda administrar el espacio
        var esDueno = reserva.SolicitanteId == usuarioId;
        var puedeAdministrarDepto = await _espacioAuthService.PuedeAdministrarDepartamentoAsync(
            reserva.Espacio.DepartamentoId,
            cancellationToken);

        if (!esDueno && !puedeAdministrarDepto)
        {
            throw new ForbiddenException("Solo el solicitante o el responsable del espacio pueden modificar esta reserva.");
        }

        // Aplicar máquina de estados para modificación (Rechazada/Cancelada -> 409; Aprobada -> PendienteAprobacion)
        var estadoAnterior = reserva.Estado;
        var nuevoEstado = ReservaStateMachine.ResolverTransicionPorModificacion(estadoAnterior);

        var inicioUtc = DateTimeZoneHelper.NormalizeToUtc(request.FechaHoraInicio);
        var finUtc = DateTimeZoneHelper.NormalizeToUtc(request.FechaHoraFin);

        // Revalidar espacio, capacidad y equipamiento
        await ObtenerYValidarEspacioCapacidadYEquipamientoAsync(
            reserva.EspacioId,
            request.AsistentesEstimados,
            request.EquipamientoRequerido,
            cancellationToken);

        // Revalidar bloqueos y superposición con reservas aprobadas (excluyendo la propia reserva)
        await ValidarSinBloqueosNiSuperposicionAprobadaAsync(
            reserva.EspacioId,
            inicioUtc,
            finUtc,
            excluirReservaId: reserva.Id,
            cancellationToken);

        reserva.FechaHoraInicio = inicioUtc;
        reserva.FechaHoraFin = finUtc;
        reserva.TipoActividad = request.TipoActividad;
        reserva.AsistentesEstimados = request.AsistentesEstimados;
        reserva.Motivo = request.Motivo.Trim();
        reserva.Observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim();
        reserva.Estado = nuevoEstado;
        reserva.ResueltoPorUsuarioId = null;
        reserva.FechaResolucion = null;
        reserva.MotivoRechazo = null;

        // Reemplazar equipamiento requerido
        _context.ReservasEquipamiento.RemoveRange(reserva.EquipamientosRequeridos);
        reserva.EquipamientosRequeridos.Clear();

        foreach (var eq in request.EquipamientoRequerido)
        {
            reserva.EquipamientosRequeridos.Add(new ReservaEquipamiento
            {
                ReservaId = reserva.Id,
                EquipamientoId = eq.EquipamientoId,
                CantidadRequerida = eq.Cantidad,
                Observaciones = string.IsNullOrWhiteSpace(eq.Observaciones) ? null : eq.Observaciones.Trim()
            });
        }

        _context.Auditorias.Add(new Auditoria
        {
            UsuarioId = usuarioId,
            FechaHora = DateTime.UtcNow,
            Accion = "ModificarReserva",
            Entidad = "Reserva",
            EntidadId = reserva.Id.ToString(),
            Motivo = reserva.Motivo,
            Detalle = estadoAnterior == EstadoReserva.Aprobada
                ? $"Reserva modificada: pasó de '{estadoAnterior}' a '{nuevoEstado}' para revalidación ({inicioUtc:yyyy-MM-dd HH:mm} a {finUtc:yyyy-MM-dd HH:mm} UTC)."
                : $"Reserva en estado '{estadoAnterior}' actualizada ({inicioUtc:yyyy-MM-dd HH:mm} a {finUtc:yyyy-MM-dd HH:mm} UTC)."
        });

        _notificacionesService.EncolarNotificacion(
            reserva.SolicitanteId,
            estadoAnterior == EstadoReserva.Aprobada
                ? $"Su reserva #{reserva.Id} para el espacio '{reserva.Espacio.Codigo}' fue modificada y volvió al estado PENDIENTE DE APROBACIÓN."
                : $"Su reserva #{reserva.Id} para el espacio '{reserva.Espacio.Codigo}' fue actualizada correctamente.");

        await _notificacionesService.EncolarNotificacionResponsablesDepartamentoAsync(
            reserva.Espacio.DepartamentoId,
            $"La reserva #{reserva.Id} del espacio '{reserva.Espacio.Codigo}' fue modificada y se encuentra pendiente de aprobación.",
            excluirUsuarioId: usuarioId,
            cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("La reserva fue modificada simultáneamente por otro usuario. Recargue e intente nuevamente.");
        }

        return await ConstruirDetalleReservaAsync(reserva.Id, cancellationToken);
    }

    private async Task<Espacio> ObtenerYValidarEspacioCapacidadYEquipamientoAsync(
        int espacioId,
        int asistentesEstimados,
        List<ReservaEquipamientoInputDto> equipamientoRequerido,
        CancellationToken cancellationToken)
    {
        var espacio = await _context.Espacios
            .AsNoTracking()
            .Include(e => e.Equipamientos)
                .ThenInclude(ee => ee.Equipamiento)
            .FirstOrDefaultAsync(e => e.Id == espacioId && e.FechaBaja == null, cancellationToken);

        if (espacio == null)
        {
            throw new NotFoundException($"No se encontró un espacio activo con identificador {espacioId}.");
        }

        if (espacio.Estado == EstadoEspacio.FueraDeServicio)
        {
            throw new ConflictException($"El espacio '{espacio.Codigo} - {espacio.Nombre}' se encuentra actualmente Fuera de Servicio y no admite reservas.");
        }

        if (asistentesEstimados > espacio.CapacidadMaxima)
        {
            throw new ValidationException(
                "AsistentesEstimados",
                $"La cantidad de asistentes estimados ({asistentesEstimados}) supera la capacidad máxima del espacio '{espacio.Codigo}' ({espacio.CapacidadMaxima} personas).");
        }

        if (equipamientoRequerido != null && equipamientoRequerido.Count > 0)
        {
            var idsSolicitados = equipamientoRequerido.Select(e => e.EquipamientoId).Distinct().ToList();
            var catalogoNombres = await _context.Equipamientos
                .AsNoTracking()
                .Where(e => idsSolicitados.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Nombre, cancellationToken);

            foreach (var req in equipamientoRequerido)
            {
                if (!catalogoNombres.TryGetValue(req.EquipamientoId, out var nombreEq))
                {
                    throw new ValidationException(
                        "EquipamientoRequerido",
                        $"El equipamiento con identificador {req.EquipamientoId} no existe en el catálogo.");
                }

                var disponibleEnEspacio = espacio.Equipamientos
                    .FirstOrDefault(ee => ee.EquipamientoId == req.EquipamientoId);

                if (disponibleEnEspacio == null)
                {
                    throw new ValidationException(
                        "EquipamientoRequerido",
                        $"El espacio '{espacio.Codigo}' no cuenta con el equipamiento requerido: '{nombreEq}'.");
                }

                if (disponibleEnEspacio.Cantidad < req.Cantidad)
                {
                    throw new ValidationException(
                        "EquipamientoRequerido",
                        $"El espacio '{espacio.Codigo}' solo dispone de {disponibleEnEspacio.Cantidad} unidad(es) de '{nombreEq}', pero se solicitaron {req.Cantidad}.");
                }
            }
        }

        return espacio;
    }

    private async Task ValidarSinBloqueosNiSuperposicionAprobadaAsync(
        int espacioId,
        DateTime inicioUtc,
        DateTime finUtc,
        int? excluirReservaId,
        CancellationToken cancellationToken)
    {
        // 1. Verificar explícitamente bloqueos activos en ese rango para dar un mensaje claro de 409
        var bloqueoConflicto = await _context.Bloqueos
            .AsNoTracking()
            .Where(b => b.EspacioId == espacioId &&
                        b.FechaHoraDesde < finUtc &&
                        b.FechaHoraHasta > inicioUtc)
            .Select(b => new { b.Motivo, b.FechaHoraDesde, b.FechaHoraHasta })
            .FirstOrDefaultAsync(cancellationToken);

        if (bloqueoConflicto != null)
        {
            throw new ConflictException(
                $"El espacio posee un bloqueo institucional en el horario solicitado (Motivo: {bloqueoConflicto.Motivo}).");
        }

        // 2. Verificar disponibilidad general (reservas aprobadas superpuestas y estado del espacio) mediante IDisponibilidadService
        var disponible = await _disponibilidadService.EstaDisponibleAsync(
            espacioId,
            inicioUtc,
            finUtc,
            excluirReservaId,
            cancellationToken);

        if (!disponible)
        {
            throw new ConflictException(
                "El espacio ya posee una reserva APROBADA que se superpone con el rango de fecha y hora solicitado.");
        }
    }

    private IQueryable<ReservaListadoDto> ProyectarListado(IQueryable<Reserva> query)
    {
        return query.Select(r => new ReservaListadoDto
        {
            Id = r.Id,
            EspacioId = r.EspacioId,
            EspacioCodigo = r.Espacio.Codigo,
            EspacioNombre = r.Espacio.Nombre,
            DepartamentoId = r.Espacio.DepartamentoId,
            DepartamentoNombre = r.Espacio.Departamento.Nombre,
            SolicitanteId = r.SolicitanteId,
            SolicitanteNombre = r.Solicitante.NombreCompleto,
            SolicitanteEmail = r.Solicitante.Email,
            FechaHoraInicio = r.FechaHoraInicio,
            FechaHoraFin = r.FechaHoraFin,
            TipoActividad = r.TipoActividad,
            AsistentesEstimados = r.AsistentesEstimados,
            Motivo = r.Motivo,
            Observaciones = r.Observaciones,
            Estado = r.Estado,
            MotivoRechazo = r.MotivoRechazo,
            FechaSolicitud = r.FechaSolicitud,
            FechaResolucion = r.FechaResolucion,
            ResueltoPorUsuarioId = r.ResueltoPorUsuarioId,
            ResueltoPorNombre = r.ResueltoPorUsuario != null ? r.ResueltoPorUsuario.NombreCompleto : null
        });
    }

    private async Task<ReservaDetalleDto> ConstruirDetalleReservaAsync(int id, CancellationToken cancellationToken)
    {
        var reserva = await _context.Reservas
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ReservaDetalleDto
            {
                Id = r.Id,
                EspacioId = r.EspacioId,
                EspacioCodigo = r.Espacio.Codigo,
                EspacioNombre = r.Espacio.Nombre,
                DepartamentoId = r.Espacio.DepartamentoId,
                DepartamentoNombre = r.Espacio.Departamento.Nombre,
                SolicitanteId = r.SolicitanteId,
                SolicitanteNombre = r.Solicitante.NombreCompleto,
                SolicitanteEmail = r.Solicitante.Email,
                FechaHoraInicio = r.FechaHoraInicio,
                FechaHoraFin = r.FechaHoraFin,
                TipoActividad = r.TipoActividad,
                AsistentesEstimados = r.AsistentesEstimados,
                Motivo = r.Motivo,
                Observaciones = r.Observaciones,
                Estado = r.Estado,
                MotivoRechazo = r.MotivoRechazo,
                FechaSolicitud = r.FechaSolicitud,
                FechaResolucion = r.FechaResolucion,
                ResueltoPorUsuarioId = r.ResueltoPorUsuarioId,
                ResueltoPorNombre = r.ResueltoPorUsuario != null ? r.ResueltoPorUsuario.NombreCompleto : null,
                EquipamientoRequerido = r.EquipamientosRequeridos
                    .OrderBy(er => er.Equipamiento.Nombre)
                    .Select(er => new ReservaEquipamientoDetalleDto
                    {
                        EquipamientoId = er.EquipamientoId,
                        Nombre = er.Equipamiento.Nombre,
                        Cantidad = er.CantidadRequerida,
                        Observaciones = er.Observaciones
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (reserva == null)
        {
            throw new NotFoundException($"No se encontró la reserva con identificador {id}.");
        }

        var entidadIdStr = id.ToString();
        reserva.HistorialCambios = await _context.Auditorias
            .AsNoTracking()
            .Where(a => a.Entidad == "Reserva" && a.EntidadId == entidadIdStr)
            .OrderBy(a => a.FechaHora)
            .ThenBy(a => a.Id)
            .Select(a => new HistorialEstadoReservaDto
            {
                Id = a.Id,
                FechaHora = a.FechaHora,
                Accion = a.Accion,
                UsuarioId = a.UsuarioId,
                UsuarioNombre = a.Usuario != null ? a.Usuario.NombreCompleto : "Sistema",
                Motivo = a.Motivo,
                Detalle = a.Detalle
            })
            .ToListAsync(cancellationToken);

        return reserva;
    }

    private static bool EsViolacionExclusionOSolape(DbUpdateException ex)
    {
        var innerMessage = ex.InnerException?.Message ?? ex.Message;
        return innerMessage.Contains("23P01", StringComparison.OrdinalIgnoreCase) ||
               innerMessage.Contains("40001", StringComparison.OrdinalIgnoreCase) ||
               innerMessage.Contains("EX_Reservas_NoOverlap_Aprobadas", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }
}


