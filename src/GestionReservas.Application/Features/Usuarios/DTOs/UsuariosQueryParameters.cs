namespace GestionReservas.Application.Features.Usuarios.DTOs;

/// <summary>
/// Parámetros de paginación y filtrado para la consulta de usuarios.
/// </summary>
public class UsuariosQueryParameters
{
    /// <summary>
    /// Número de página solicitada (base 1).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Cantidad de registros por página (máximo 100).
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Filtro de texto opcional aplicado a nombre, apellido o correo electrónico.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filtro por nombre de rol específico asignado.
    /// </summary>
    public string? Rol { get; set; }

    /// <summary>
    /// Filtro por identificador de departamento asignado.
    /// </summary>
    public int? DepartamentoId { get; set; }

    /// <summary>
    /// Filtro opcional por estado activo/inactivo.
    /// </summary>
    public bool? Activo { get; set; }
}
