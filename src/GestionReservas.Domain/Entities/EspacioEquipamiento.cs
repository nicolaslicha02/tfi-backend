namespace GestionReservas.Domain.Entities;

/// <summary>
/// Tabla intermedia que asocia un equipamiento a un espacio físico con su cantidad y detalle opcional.
/// Evita columnas fijas rígidas y permite un catálogo flexible y extensible.
/// </summary>
public class EspacioEquipamiento
{
    public int EspacioId { get; set; }
    public Espacio Espacio { get; set; } = null!;

    public int EquipamientoId { get; set; }
    public Equipamiento Equipamiento { get; set; } = null!;

    /// <summary>
    /// Cantidad de unidades de este equipamiento instaladas o disponibles en el espacio.
    /// </summary>
    public int Cantidad { get; set; } = 1;

    /// <summary>
    /// Valor opcional, detalle de configuración o modelo particular (ej: "Resolución 4K", "Marca Epson mod X").
    /// </summary>
    public string? ValorOpcional { get; set; }

    /// <summary>
    /// Observaciones sobre el estado o uso del equipamiento en dicho espacio.
    /// </summary>
    public string? Observaciones { get; set; }
}
