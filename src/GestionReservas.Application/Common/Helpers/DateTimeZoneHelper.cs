namespace GestionReservas.Application.Common.Helpers;

/// <summary>
/// Helper centralizado para conversión y normalización de fechas y zonas horarias.
/// La facultad opera bajo la zona horaria institucional de Buenos Aires (America/Argentina/Buenos_Aires).
/// Todas las fechas de base de datos se persisten y consultan en UTC conforme a los requerimientos de Npgsql.
/// </summary>
public static class DateTimeZoneHelper
{
    /// <summary>
    /// Zona horaria oficial de la facultad (America/Argentina/Buenos_Aires).
    /// </summary>
    public static readonly TimeZoneInfo FacultadTimeZone = ResolveFacultadTimeZone();

    private static TimeZoneInfo ResolveFacultadTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
            }
            catch
            {
                return TimeZoneInfo.CreateCustomTimeZone(
                    "Facultad_ART",
                    TimeSpan.FromHours(-3),
                    "Hora Oficial de Argentina",
                    "Hora Oficial de Argentina");
            }
        }
    }

    /// <summary>
    /// Convierte una fecha y hora local de la facultad a DateTime UTC con Kind = DateTimeKind.Utc.
    /// </summary>
    public static DateTime ToUtc(DateOnly fecha, TimeOnly hora)
    {
        var localDateTime = fecha.ToDateTime(hora);
        var unspecifiedLocal = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(unspecifiedLocal, FacultadTimeZone);
        return DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
    }

    /// <summary>
    /// Convierte una fecha y hora UTC a la hora local correspondiente de la facultad.
    /// </summary>
    public static DateTime ToFacultadLocal(DateTime utcDateTime)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, FacultadTimeZone);
    }

    /// <summary>
    /// Normaliza cualquier DateTime (UTC, Local o Unspecified asumido como hora de la Facultad) a DateTimeKind.Utc.
    /// </summary>
    public static DateTime NormalizeToUtc(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(dateTime, FacultadTimeZone), DateTimeKind.Utc)
        };
    }
}
