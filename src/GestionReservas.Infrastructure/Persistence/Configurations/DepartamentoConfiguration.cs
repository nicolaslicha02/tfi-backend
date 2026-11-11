using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class DepartamentoConfiguration : IEntityTypeConfiguration<Departamento>
{
    public void Configure(EntityTypeBuilder<Departamento> builder)
    {
        builder.ToTable("Departamentos");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Nombre)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(d => d.Nombre)
            .IsUnique();

        builder.Property(d => d.Descripcion)
            .HasMaxLength(300);
    }
}
