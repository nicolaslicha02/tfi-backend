using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class AuditoriaConfiguration : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        builder.ToTable("Auditorias");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FechaHora)
            .IsRequired();

        builder.Property(a => a.Accion)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Entidad)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntidadId)
            .HasMaxLength(100);

        builder.Property(a => a.Motivo)
            .HasMaxLength(500);

        builder.Property(a => a.Detalle)
            .HasColumnType("text");

        builder.HasOne(a => a.Usuario)
            .WithMany(u => u.Auditorias)
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.FechaHora);
        builder.HasIndex(a => new { a.Entidad, a.EntidadId });
    }
}
