using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class EspacioEquipamientoConfiguration : IEntityTypeConfiguration<EspacioEquipamiento>
{
    public void Configure(EntityTypeBuilder<EspacioEquipamiento> builder)
    {
        builder.ToTable("EspacioEquipamientos", t =>
        {
            t.HasCheckConstraint("CK_EspacioEquipamiento_CantidadPositiva", "\"Cantidad\" > 0");
        });

        builder.HasKey(ee => new { ee.EspacioId, ee.EquipamientoId });

        builder.HasOne(ee => ee.Espacio)
            .WithMany(e => e.Equipamientos)
            .HasForeignKey(ee => ee.EspacioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ee => ee.Equipamiento)
            .WithMany(eq => eq.EspaciosEquipamiento)
            .HasForeignKey(ee => ee.EquipamientoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ee => ee.Cantidad)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(ee => ee.ValorOpcional)
            .HasMaxLength(200);

        builder.Property(ee => ee.Observaciones)
            .HasMaxLength(300);
    }
}
