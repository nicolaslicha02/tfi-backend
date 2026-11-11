using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class TipoEspacioConfiguration : IEntityTypeConfiguration<TipoEspacio>
{
    public void Configure(EntityTypeBuilder<TipoEspacio> builder)
    {
        builder.ToTable("TiposEspacio");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => t.Nombre)
            .IsUnique();

        builder.Property(t => t.Descripcion)
            .HasMaxLength(300);
    }
}
