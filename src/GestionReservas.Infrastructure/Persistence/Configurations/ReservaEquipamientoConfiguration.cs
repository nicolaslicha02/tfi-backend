using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class ReservaEquipamientoConfiguration : IEntityTypeConfiguration<ReservaEquipamiento>
{
    public void Configure(EntityTypeBuilder<ReservaEquipamiento> builder)
    {
        builder.ToTable("ReservaEquipamientos", t =>
        {
            t.HasCheckConstraint("CK_ReservaEquipamiento_CantidadPositiva", "\"CantidadRequerida\" > 0");
        });

        builder.HasKey(re => new { re.ReservaId, re.EquipamientoId });

        builder.HasOne(re => re.Reserva)
            .WithMany(r => r.EquipamientosRequeridos)
            .HasForeignKey(re => re.ReservaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(re => re.Equipamiento)
            .WithMany(eq => eq.ReservasEquipamiento)
            .HasForeignKey(re => re.EquipamientoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(re => re.CantidadRequerida)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(re => re.Observaciones)
            .HasMaxLength(300);
    }
}
