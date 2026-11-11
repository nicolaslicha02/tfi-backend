using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.ToTable("Reservas", t =>
        {
            t.HasCheckConstraint("CK_Reserva_FinMayorInicio", "\"FechaHoraFin\" > \"FechaHoraInicio\"");
            t.HasCheckConstraint("CK_Reserva_AsistentesPositivos", "\"AsistentesEstimados\" > 0");
        });

        builder.HasKey(r => r.Id);

        // Índice compuesto solicitado para búsquedas y prevención de solapes
        builder.HasIndex(r => new { r.EspacioId, r.FechaHoraInicio, r.FechaHoraFin });

        // Concurrencia optimista mediante xmin (PostgreSQL)
        builder.Property(r => r.RowVersion)
            .IsRowVersion();

        builder.Property(r => r.FechaHoraInicio)
            .IsRequired();

        builder.Property(r => r.FechaHoraFin)
            .IsRequired();

        builder.Property(r => r.TipoActividad)
            .IsRequired();

        builder.Property(r => r.AsistentesEstimados)
            .IsRequired();

        builder.Property(r => r.Motivo)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(r => r.Observaciones)
            .HasMaxLength(500);

        builder.Property(r => r.Estado)
            .IsRequired();

        builder.Property(r => r.FechaResolucion);

        builder.Property(r => r.MotivoRechazo)
            .HasMaxLength(500);

        builder.Property(r => r.FechaSolicitud)
            .IsRequired();

        builder.HasOne(r => r.Solicitante)
            .WithMany(u => u.ReservasSolicitadas)
            .HasForeignKey(r => r.SolicitanteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Espacio)
            .WithMany(e => e.Reservas)
            .HasForeignKey(r => r.EspacioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ResueltoPorUsuario)
            .WithMany(u => u.ReservasResueltas)
            .HasForeignKey(r => r.ResueltoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
