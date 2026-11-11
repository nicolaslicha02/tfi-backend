using GestionReservas.Domain.Entities;

namespace GestionReservas.Application.Features.Espacios;

/// <summary>
/// Contrato del servicio de autorización departamental para administración de espacios físicos.
/// </summary>
public interface IEspacioAuthorizationService
{
    /// <summary>
    /// Verifica si el usuario autenticado tiene facultades para administrar (crear, editar, eliminar)
    /// un espacio asignado a un departamento determinado.
    /// - Administrador y SecretaríaAcademica: pueden administrar cualquier departamento.
    /// - ResponsableEspacio: solo puede administrar espacios de su propio departamento asignado.
    /// - Solicitante u otros: denegado.
    /// </summary>
    Task<bool> PuedeAdministrarDepartamentoAsync(int departamentoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica la autorización y arroja ForbiddenException (403) si el usuario no tiene permisos sobre el departamento.
    /// </summary>
    Task ValidarPuedeAdministrarDepartamentoAsync(int departamentoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sobrecarga conveniente para evaluar directamente un espacio físico existente.
    /// </summary>
    Task ValidarPuedeAdministrarEspacioAsync(Espacio espacio, CancellationToken cancellationToken = default);
}
