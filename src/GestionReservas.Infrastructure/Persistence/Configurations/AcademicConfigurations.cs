using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class CicloLectivoConfiguration : IEntityTypeConfiguration<CicloLectivo>
{
    public void Configure(EntityTypeBuilder<CicloLectivo> builder)
    {
        builder.ToTable("CiclosLectivos");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Anio)
            .IsRequired();

        builder.HasIndex(c => c.Anio)
            .IsUnique();

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.FechaInicio)
            .IsRequired();

        builder.Property(c => c.FechaFin)
            .IsRequired();

        builder.Property(c => c.Activo)
            .IsRequired()
            .HasDefaultValue(true);
    }
}

public class MateriaConfiguration : IEntityTypeConfiguration<Materia>
{
    public void Configure(EntityTypeBuilder<Materia> builder)
    {
        builder.ToTable("Materias");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Codigo)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(m => m.Codigo)
            .IsUnique();

        builder.Property(m => m.Nombre)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(m => m.Departamento)
            .WithMany(d => d.Materias)
            .HasForeignKey(m => m.DepartamentoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CursadaConfiguration : IEntityTypeConfiguration<Cursada>
{
    public void Configure(EntityTypeBuilder<Cursada> builder)
    {
        builder.ToTable("Cursadas");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Periodo)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(c => c.Materia)
            .WithMany(m => m.Cursadas)
            .HasForeignKey(c => c.MateriaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.CicloLectivo)
            .WithMany(cl => cl.Cursadas)
            .HasForeignKey(c => c.CicloLectivoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ComisionConfiguration : IEntityTypeConfiguration<Comision>
{
    public void Configure(EntityTypeBuilder<Comision> builder)
    {
        builder.ToTable("Comisiones");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Codigo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Turno)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.CantidadInscriptos)
            .IsRequired();

        builder.HasOne(c => c.Cursada)
            .WithMany(cu => cu.Comisiones)
            .HasForeignKey(c => c.CursadaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AsignacionEspacioConfiguration : IEntityTypeConfiguration<AsignacionEspacio>
{
    public void Configure(EntityTypeBuilder<AsignacionEspacio> builder)
    {
        builder.ToTable("AsignacionesEspacio");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.DiaSemana)
            .IsRequired();

        builder.Property(a => a.HoraInicio)
            .IsRequired();

        builder.Property(a => a.HoraFin)
            .IsRequired();

        builder.HasOne(a => a.Comision)
            .WithMany(c => c.Asignaciones)
            .HasForeignKey(a => a.ComisionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Espacio)
            .WithMany(e => e.Asignaciones)
            .HasForeignKey(a => a.EspacioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice compuesto para validación de cronograma académico oficial
        builder.HasIndex(a => new { a.EspacioId, a.DiaSemana, a.HoraInicio, a.HoraFin });
    }
}

public class HistorialMatriculaConfiguration : IEntityTypeConfiguration<HistorialMatricula>
{
    public void Configure(EntityTypeBuilder<HistorialMatricula> builder)
    {
        builder.ToTable("HistorialesMatricula");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.FechaRegistro)
            .IsRequired();

        builder.Property(h => h.CantidadAlumnosPresentes)
            .IsRequired();

        builder.Property(h => h.Observaciones)
            .HasMaxLength(300);

        builder.HasOne(h => h.Comision)
            .WithMany(c => c.HistorialMatriculas)
            .HasForeignKey(h => h.ComisionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
