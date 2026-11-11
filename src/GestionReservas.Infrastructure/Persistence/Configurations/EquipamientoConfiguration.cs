using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class EquipamientoConfiguration : IEntityTypeConfiguration<Equipamiento>
{
    public void Configure(EntityTypeBuilder<Equipamiento> builder)
    {
        builder.ToTable("Equipamientos");

        builder.HasKey(eq => eq.Id);

        builder.Property(eq => eq.Nombre)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(eq => eq.Nombre)
            .IsUnique();

        builder.Property(eq => eq.Descripcion)
            .HasMaxLength(300);
    }
}
