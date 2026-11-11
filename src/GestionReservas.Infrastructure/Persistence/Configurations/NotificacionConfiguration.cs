using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.ToTable("Notificaciones");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Mensaje)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.Leida)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.Fecha)
            .IsRequired();

        builder.HasOne(n => n.Usuario)
            .WithMany(u => u.Notificaciones)
            .HasForeignKey(n => n.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
