using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class EspacioConfiguration : IEntityTypeConfiguration<Espacio>
{
    public void Configure(EntityTypeBuilder<Espacio> builder)
    {
        builder.ToTable("Espacios", t =>
        {
            t.HasCheckConstraint("CK_Espacio_CapacidadPositiva", "\"CapacidadMaxima\" > 0");
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Codigo)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.Codigo)
            .IsUnique();

        builder.Property(e => e.Nombre)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Edificio)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Piso)
            .IsRequired();

        builder.Property(e => e.Sector)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.CapacidadMaxima)
            .IsRequired();

        builder.Property(e => e.Estado)
            .IsRequired();

        builder.Property(e => e.FechaBaja);

        builder.Ignore(e => e.EsBaja);

        builder.HasOne(e => e.Departamento)
            .WithMany(d => d.Espacios)
            .HasForeignKey(e => e.DepartamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TipoEspacio)
            .WithMany(t => t.Espacios)
            .HasForeignKey(e => e.TipoEspacioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
