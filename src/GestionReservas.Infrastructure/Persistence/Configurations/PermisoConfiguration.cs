using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class PermisoConfiguration : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> builder)
    {
        builder.ToTable("Permisos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Codigo)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.Codigo)
            .IsUnique();

        builder.Property(p => p.Descripcion)
            .HasMaxLength(250);
    }
}
