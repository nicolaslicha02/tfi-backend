using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Usuarios.DTOs;

namespace GestionReservas.Application.Features.Usuarios;

/// <summary>
/// Contrato del servicio de administración de cuentas de usuario, asignación de roles y estados.
/// </summary>
public interface IUsuariosService
{
    /// <summary>
    /// Crea un nuevo usuario en el sistema registrando la auditoría correspondiente.
    /// </summary>
    Task<UsuarioDetalleDto> CrearUsuarioAsync(CrearUsuarioRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un listado paginado de usuarios con opciones de búsqueda y filtrado.
    /// </summary>
    Task<PagedResultDto<UsuarioListadoDto>> GetUsuariosAsync(UsuariosQueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el detalle completo de un usuario por su identificador.
    /// </summary>
    Task<UsuarioDetalleDto> GetUsuarioByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza parcialmente los datos, roles o estado de activación de un usuario existente.
    /// </summary>
    Task<UsuarioDetalleDto> ActualizarUsuarioAsync(int id, ActualizarUsuarioRequestDto request, CancellationToken cancellationToken = default);
}
