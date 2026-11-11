namespace GestionReservas.Domain.Enums;

/// <summary>
/// Representa la disponibilidad operativa de un espacio físico.
/// </summary>
public enum EstadoEspacio
{
    /// <summary>
    /// El espacio se encuentra operativo y apto para ser reservado.
    /// </summary>
    Disponible = 1,

    /// <summary>
    /// El espacio se encuentra inhabilitado por refacciones, fallas o mantenimiento prolongado.
    /// </summary>
    FueraDeServicio = 2
}
