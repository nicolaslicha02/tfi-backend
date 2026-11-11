using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class BloqueoConfiguration : IEntityTypeConfiguration<Bloqueo>
{
    public void Configure(EntityTypeBuilder<Bloqueo> builder)
    {
        builder.ToTable("Bloqueos", t =>
        {
            t.HasCheckConstraint("CK_Bloqueo_HastaMayorDesde", "\"FechaHoraHasta\" > \"FechaHoraDesde\"");
        });

        builder.HasKey(b => b.Id);

        // Índice compuesto para validación de disponibilidad y superposiciones de bloqueos
        builder.HasIndex(b => new { b.EspacioId, b.FechaHoraDesde, b.FechaHoraHasta });

        builder.Property(b => b.FechaHoraDesde)
            .IsRequired();

        builder.Property(b => b.FechaHoraHasta)
            .IsRequired();

        builder.Property(b => b.Motivo)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(b => b.FechaCreacion)
            .IsRequired();

        builder.HasOne(b => b.Espacio)
            .WithMany(e => e.Bloqueos)
            .HasForeignKey(b => b.EspacioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.CreadoPorUsuario)
            .WithMany(u => u.BloqueosCreados)
            .HasForeignKey(b => b.CreadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
