using GestionReservas.Domain.Entities;
using GestionReservas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionReservas.Tests.Infrastructure;

internal static class AcademicTestData
{
    public static async Task<string> AgregarAsignacionAsync(
        ApplicationDbContext db,
        int espacioId,
        DateOnly fecha,
        DayOfWeek diaSemana,
        TimeOnly inicio,
        TimeOnly fin)
    {
        int anio;
        do
        {
            anio = Random.Shared.Next(10_000, int.MaxValue);
        } while (await db.CiclosLectivos.AnyAsync(c => c.Anio == anio));

        var codigo = $"TEST-{Guid.NewGuid():N}"[..15];
        var desde = DateTime.SpecifyKind(fecha.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var hasta = DateTime.SpecifyKind(fecha.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

        var asignacion = new AsignacionEspacio
        {
            EspacioId = espacioId,
            DiaSemana = diaSemana,
            HoraInicio = inicio.ToTimeSpan(),
            HoraFin = fin.ToTimeSpan(),
            Comision = new Comision
            {
                Codigo = codigo,
                Turno = "Noche",
                Modalidad = "Presencial",
                CantidadInscriptos = 20,
                Cursada = new Cursada
                {
                    Periodo = "Prueba automatizada",
                    Materia = new Materia
                    {
                        Codigo = codigo,
                        Nombre = "Materia de prueba automatizada",
                        DepartamentoId = 2
                    },
                    CicloLectivo = new CicloLectivo
                    {
                        Anio = anio,
                        Nombre = $"Ciclo de prueba {anio}",
                        FechaInicio = desde,
                        FechaFin = hasta,
                        Activo = true
                    }
                }
            }
        };

        db.AsignacionesEspacio.Add(asignacion);
        await db.SaveChangesAsync();
        return codigo;
    }
}
