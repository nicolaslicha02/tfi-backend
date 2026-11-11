using GestionReservas.Domain.Enums;

namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa una infraestructura física concreta de la facultad (aula, laboratorio, auditorio o sala).
/// </summary>
public class Espacio
{
    public int Id { get; set; }

    /// <summary>
    /// Código alfanumérico institucional único (ej: "AULA-101", "LAB-SIS-01").
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre descriptivo del espacio.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Identificación del edificio físico donde se ubica el espacio.
    /// </summary>
    public string Edificio { get; set; } = string.Empty;

    /// <summary>
    /// Número de piso dentro del edificio.
    /// </summary>
    public int Piso { get; set; }

    /// <summary>
    /// Sector, ala o pasillo específico (ej: "Ala Norte", "Sector Central").
    /// </summary>
    public string Sector { get; set; } = string.Empty;

    /// <summary>
    /// Aforo o capacidad máxima de personas sentadas (debe ser estrictamente mayor a 0).
    /// </summary>
    public int CapacidadMaxima { get; set; }

    /// <summary>
    /// Estado operativo del espacio (Disponible o FueraDeServicio).
    /// </summary>
    public EstadoEspacio Estado { get; set; } = EstadoEspacio.Disponible;

    /// <summary>
    /// Identificador del departamento o secretaría a cargo de su gestión.
    /// </summary>
    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;

    /// <summary>
    /// Tipo o categoría de espacio.
    /// </summary>
    public int TipoEspacioId { get; set; }
    public TipoEspacio TipoEspacio { get; set; } = null!;

    /// <summary>
    /// Fecha de baja lógica del espacio. Si es null, el espacio se encuentra activo.
    /// </summary>
    public DateTime? FechaBaja { get; set; }

    /// <summary>
    /// Indica si el espacio fue dado de baja lógicamente.
    /// </summary>
    public bool EsBaja => FechaBaja.HasValue;

    // Relaciones
    public ICollection<EspacioEquipamiento> Equipamientos { get; set; } = new List<EspacioEquipamiento>();
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    public ICollection<Bloqueo> Bloqueos { get; set; } = new List<Bloqueo>();
    public ICollection<AsignacionEspacio> Asignaciones { get; set; } = new List<AsignacionEspacio>();
}
