namespace GestionReservas.Domain.Constants;

/// <summary>
/// Catálogo centralizado de códigos de permisos del sistema para verificación y registro dinámico de políticas.
/// </summary>
public static class PermisosDefinidos
{
    // Espacios
    public const string EspaciosVer = "espacios.ver";
    public const string EspaciosCrear = "espacios.crear";
    public const string EspaciosEditar = "espacios.editar";
    public const string EspaciosEliminar = "espacios.eliminar";

    // Reservas
    public const string ReservasSolicitar = "reservas.solicitar";
    public const string ReservasVerPropias = "reservas.ver_propias";
    public const string ReservasVerPendientes = "reservas.ver_pendientes";
    public const string ReservasAprobarRechazar = "reservas.aprobar_rechazar";
    public const string ReservasCancelar = "reservas.cancelar";
    public const string ReservasModificar = "reservas.modificar";

    // Bloqueos
    public const string BloqueosVer = "bloqueos.ver";
    public const string BloqueosCrear = "bloqueos.crear";

    // Catálogos y Usuarios
    public const string CatalogosVer = "catalogos.ver";
    public const string AuditoriaVer = "auditoria.ver";
    public const string UsuariosGestionar = "usuarios.gestionar";

    /// <summary>
    /// Lista inmutable con todos los permisos del sistema sembrados en la base de datos.
    /// </summary>
    public static readonly IReadOnlyList<string> Todos = new[]
    {
        EspaciosVer,
        EspaciosCrear,
        EspaciosEditar,
        EspaciosEliminar,
        ReservasSolicitar,
        ReservasVerPropias,
        ReservasVerPendientes,
        ReservasAprobarRechazar,
        ReservasCancelar,
        ReservasModificar,
        BloqueosVer,
        BloqueosCrear,
        CatalogosVer,
        AuditoriaVer,
        UsuariosGestionar
    };
}
