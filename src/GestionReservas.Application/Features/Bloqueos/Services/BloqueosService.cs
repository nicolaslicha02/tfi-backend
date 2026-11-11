using FluentValidation;
using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Bloqueos.DTOs;
using GestionReservas.Application.Features.Espacios;
using GestionReservas.Application.Features.Notificaciones;
using GestionReservas.Application.Features.Reservas;
using GestionReservas.Domain.Entities;
using GestionReservas.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ValidationException = GestionReservas.Application.Common.Exceptions.ValidationException;

namespace GestionReservas.Application.Features.Bloqueos.Services;

/// <summary>
/// Implementación de la lógica de negocio para la gestión de bloqueos temporales de espacios.
/// </summary>
public class BloqueosService : IBloqueosService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEspacioAuthorizationService _espacioAuthService;
    private readonly INotificacionesService _notificacionesService;
    private readonly IValidator<CrearBloqueoRequestDto> _crearValidator;
    private readonly IValidator<BloqueosQueryParameters> _queryParamsValidator;

    public BloqueosService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IEspacioAuthorizationService espacioAuthService,
        INotificacionesService notificacionesService,
        IValidator<CrearBloqueoRequestDto> crearValidator,
        IValidator<BloqueosQueryParameters> queryParamsValidator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _espacioAuthService = espacioAuthService;
        _notificacionesService = notificacionesService;
        _crearValidator = crearValidator;
        _queryParamsValidator = queryParamsValidator;
    }

    public async Task<CrearBloqueoResponseDto> CrearBloqueoAsync(CrearBloqueoRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _crearValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var usuarioId = _currentUserService.UsuarioId
            ?? throw new UnauthorizedException("Usuario no autenticado.");

        var desdeUtc = DateTimeZoneHelper.NormalizeToUtc(request.Desde);
        var hastaUtc = DateTimeZoneHelper.NormalizeToUtc(request.Hasta);

        // 1. Espacio existente y no dado de baja
        var espacio = await _context.Espacios
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EspacioId && e.FechaBaja == null, cancellationToken);

        if (espacio == null)
        {
            throw new NotFoundException($"No se encontró un espacio activo con identificador {request.EspacioId}.");
        }

        // 2. Permisos sobre el departamento del espacio (Admin y Secretaría cualquier espacio; Responsable solo de su depto)
        await _espacioAuthService.ValidarPuedeAdministrarDepartamentoAsync(espacio.DepartamentoId, cancellationToken);

        // 3. Transacción única con advisory lock para serializar operaciones concurrentes sobre el mismo espacio
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _context.AcquireAdvisoryXactLockAsync(espacio.Id, cancellationToken);

            // 4. Validar que no existan bloqueos superpuestos sobre el mismo espacio (criterio: inicioA < finB AND finA > inicioB)
            var bloqueoSuperpuesto = await _context.Bloqueos
                .AsNoTracking()
                .AnyAsync(b => b.EspacioId == espacio.Id &&
                               b.FechaHoraDesde < hastaUtc &&
                               b.FechaHoraHasta > desdeUtc,
                          cancellationToken);

            if (bloqueoSuperpuesto)
            {
                throw new ConflictException("El espacio ya posee un bloqueo registrado que se superpone con el período indicado.");
            }

            // 5. Detectar reservas afectadas (APROBADAS o PENDIENTES que se superponen con el rango)
            var reservasAfectadas = await _context.Reservas
                .Include(r => r.Solicitante)
                .Where(r => r.EspacioId == espacio.Id &&
                            (r.Estado == EstadoReserva.Aprobada || r.Estado == EstadoReserva.PendienteAprobacion) &&
                            r.FechaHoraInicio < hastaUtc &&
                            r.FechaHoraFin > desdeUtc)
                .OrderBy(r => r.FechaHoraInicio)
                .ToListAsync(cancellationToken);

            // 6. Si existen afectadas y no se solicitó cancelarlas, retornar 409 Conflict con la lista detallada
            if (reservasAfectadas.Count > 0 && !request.CancelarReservasAfectadas)
            {
                var afectadasDto = reservasAfectadas.Select(r => new ReservaAfectadaDto
                {
                    Id = r.Id,
                    SolicitanteId = r.SolicitanteId,
                    Solicitante = r.Solicitante.NombreCompleto,
                    SolicitanteEmail = r.Solicitante.Email,
                    FechaHoraInicio = r.FechaHoraInicio,
                    FechaHoraFin = r.FechaHoraFin,
                    Estado = r.Estado
                }).ToList();

                throw new ConflictException(
                    $"No se puede crear el bloqueo porque existen {afectadasDto.Count} reserva(s) afectada(s) en ese horario. Indique 'cancelarReservasAfectadas = true' para cancelarlas automáticamente.",
                    afectadasDto);
            }

            var ahoraUtc = DateTime.UtcNow;
            var reservasCanceladasDto = new List<ReservaAfectadaDto>();

            // 7. Si cancelarReservasAfectadas = true, cancelar cada una usando la máquina de estados, auditar y notificar
            if (reservasAfectadas.Count > 0)
            {
                foreach (var reserva in reservasAfectadas)
                {
                    var estadoAnterior = reserva.Estado;
                    var nuevoEstado = ReservaStateMachine.ResolverTransicion(estadoAnterior, ReservaStateMachine.AccionCancelar);
                    var motivoCancelacion = $"Cancelada por bloqueo del espacio: {request.Motivo.Trim()}";

                    reserva.Estado = nuevoEstado;
                    reserva.FechaResolucion = ahoraUtc;
                    reserva.ResueltoPorUsuarioId = usuarioId;
                    reserva.MotivoRechazo = motivoCancelacion;

                    _context.Auditorias.Add(new Auditoria
                    {
                        UsuarioId = usuarioId,
                        FechaHora = ahoraUtc,
                        Accion = "CancelarReserva",
                        Entidad = "Reserva",
                        EntidadId = reserva.Id.ToString(),
                        Motivo = motivoCancelacion,
                        Detalle = $"Cancelación automática de reserva #{reserva.Id} ('{estadoAnterior}') por creación de bloqueo en espacio '{espacio.Codigo}' ({desdeUtc:yyyy-MM-dd HH:mm} a {hastaUtc:yyyy-MM-dd HH:mm} UTC)."
                    });

                    _notificacionesService.EncolarNotificacion(
                        reserva.SolicitanteId,
                        $"Su reserva #{reserva.Id} para el espacio '{espacio.Codigo}' ha sido CANCELADA debido a un bloqueo del espacio: {request.Motivo.Trim()}");

                    reservasCanceladasDto.Add(new ReservaAfectadaDto
                    {
                        Id = reserva.Id,
                        SolicitanteId = reserva.SolicitanteId,
                        Solicitante = reserva.Solicitante.NombreCompleto,
                        SolicitanteEmail = reserva.Solicitante.Email,
                        FechaHoraInicio = reserva.FechaHoraInicio,
                        FechaHoraFin = reserva.FechaHoraFin,
                        Estado = nuevoEstado
                    });
                }
            }

            // 8. Crear y persistir el bloqueo
            var bloqueo = new Bloqueo
            {
                EspacioId = espacio.Id,
                FechaHoraDesde = desdeUtc,
                FechaHoraHasta = hastaUtc,
                Motivo = request.Motivo.Trim(),
                CreadoPorUsuarioId = usuarioId,
                FechaCreacion = ahoraUtc
            };

            _context.Bloqueos.Add(bloqueo);
            await _context.SaveChangesAsync(cancellationToken);

            // 9. Auditar la creación del bloqueo
            _context.Auditorias.Add(new Auditoria
            {
                UsuarioId = usuarioId,
                FechaHora = ahoraUtc,
                Accion = "CrearBloqueo",
                Entidad = "Bloqueo",
                EntidadId = bloqueo.Id.ToString(),
                Motivo = bloqueo.Motivo,
                Detalle = $"Bloqueo creado en espacio '{espacio.Codigo}' desde {desdeUtc:yyyy-MM-dd HH:mm} hasta {hastaUtc:yyyy-MM-dd HH:mm} UTC. Reservas canceladas: {reservasCanceladasDto.Count}."
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var usuarioNombre = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == usuarioId)
                .Select(u => u.NombreCompleto)
                .FirstOrDefaultAsync(cancellationToken) ?? "Sistema";

            return new CrearBloqueoResponseDto
            {
                Id = bloqueo.Id,
                EspacioId = espacio.Id,
                EspacioCodigo = espacio.Codigo,
                EspacioNombre = espacio.Nombre,
                FechaHoraDesde = bloqueo.FechaHoraDesde,
                FechaHoraHasta = bloqueo.FechaHoraHasta,
                Motivo = bloqueo.Motivo,
                CreadoPorUsuarioId = usuarioId,
                CreadoPorUsuarioNombre = usuarioNombre,
                FechaCreacion = bloqueo.FechaCreacion,
                ReservasCanceladas = reservasCanceladasDto
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResultDto<BloqueoListadoDto>> GetBloqueosAsync(BloqueosQueryParameters queryParams, CancellationToken cancellationToken = default)
    {
        var validationResult = await _queryParamsValidator.ValidateAsync(queryParams, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var usuarioId = _currentUserService.UsuarioId
            ?? throw new UnauthorizedException("Usuario no autenticado.");

        var query = _context.Bloqueos
            .AsNoTracking()
            .Include(b => b.Espacio)
                .ThenInclude(e => e.Departamento)
            .Include(b => b.CreadoPorUsuario)
            .AsQueryable();

        // Alcance departamental: Admin y Secretaría ven todos; Responsable solo de su depto
        var esAdminOSecretaria = _currentUserService.IsInRole("Administrador") ||
                                 _currentUserService.IsInRole("SecretariaAcademica");

        if (!esAdminOSecretaria)
        {
            if (!_currentUserService.IsInRole("ResponsableEspacio"))
            {
                throw new ForbiddenException("No tiene permisos para consultar los bloqueos de espacios.");
            }

            var deptoId = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == usuarioId)
                .Select(u => u.DepartamentoId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!deptoId.HasValue)
            {
                return new PagedResultDto<BloqueoListadoDto>(Array.Empty<BloqueoListadoDto>(), 0, queryParams.Page, queryParams.PageSize);
            }

            query = query.Where(b => b.Espacio.DepartamentoId == deptoId.Value);
        }

        if (queryParams.EspacioId.HasValue)
        {
            query = query.Where(b => b.EspacioId == queryParams.EspacioId.Value);
        }

        if (queryParams.Desde.HasValue)
        {
            var desdeUtc = DateTimeZoneHelper.NormalizeToUtc(queryParams.Desde.Value);
            query = query.Where(b => b.FechaHoraHasta >= desdeUtc);
        }

        if (queryParams.Hasta.HasValue)
        {
            var hastaUtc = DateTimeZoneHelper.NormalizeToUtc(queryParams.Hasta.Value);
            query = query.Where(b => b.FechaHoraDesde <= hastaUtc);
        }

        if (queryParams.Vigentes.HasValue)
        {
            var ahora = DateTime.UtcNow;
            query = queryParams.Vigentes.Value
                ? query.Where(b => b.FechaHoraHasta > ahora)
                : query.Where(b => b.FechaHoraHasta <= ahora);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(b => b.FechaHoraDesde)
            .ThenByDescending(b => b.Id)
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(b => new BloqueoListadoDto
            {
                Id = b.Id,
                EspacioId = b.EspacioId,
                EspacioCodigo = b.Espacio.Codigo,
                EspacioNombre = b.Espacio.Nombre,
                DepartamentoId = b.Espacio.DepartamentoId,
                DepartamentoNombre = b.Espacio.Departamento.Nombre,
                FechaHoraDesde = b.FechaHoraDesde,
                FechaHoraHasta = b.FechaHoraHasta,
                Motivo = b.Motivo,
                CreadoPorUsuarioId = b.CreadoPorUsuarioId,
                CreadoPorUsuarioNombre = b.CreadoPorUsuario.NombreCompleto,
                FechaCreacion = b.FechaCreacion
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<BloqueoListadoDto>(items, total, queryParams.Page, queryParams.PageSize);
    }

    public async Task EliminarBloqueoAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId
            ?? throw new UnauthorizedException("Usuario no autenticado.");

        var bloqueo = await _context.Bloqueos
            .Include(b => b.Espacio)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (bloqueo == null)
        {
            throw new NotFoundException($"No se encontró el bloqueo con identificador {id}.");
        }

        // Permisos departamentales
        await _espacioAuthService.ValidarPuedeAdministrarDepartamentoAsync(bloqueo.Espacio.DepartamentoId, cancellationToken);

        _context.Bloqueos.Remove(bloqueo);

        _context.Auditorias.Add(new Auditoria
        {
            UsuarioId = usuarioId,
            FechaHora = DateTime.UtcNow,
            Accion = "LevantarBloqueo",
            Entidad = "Bloqueo",
            EntidadId = id.ToString(),
            Motivo = bloqueo.Motivo,
            Detalle = $"Bloqueo en espacio '{bloqueo.Espacio.Codigo}' levantado exitosamente. Nota: Las reservas canceladas por el bloqueo no son restauradas automáticamente."
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
