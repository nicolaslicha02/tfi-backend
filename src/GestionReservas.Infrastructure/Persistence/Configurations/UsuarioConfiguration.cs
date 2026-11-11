using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nombre)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty);

        builder.Property(u => u.Apellido)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty);

        builder.Property(u => u.NombreCompleto)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.Telefono)
            .HasMaxLength(30);

        builder.Property(u => u.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.FechaCreacion)
            .IsRequired();

        builder.HasOne(u => u.Departamento)
            .WithMany(d => d.Usuarios)
            .HasForeignKey(u => u.DepartamentoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
