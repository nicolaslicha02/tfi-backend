using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionReservas.Infrastructure.Persistence.Configurations;

public class RolPermisoConfiguration : IEntityTypeConfiguration<RolPermiso>
{
    public void Configure(EntityTypeBuilder<RolPermiso> builder)
    {
        builder.ToTable("RolPermisos");

        builder.HasKey(rp => new { rp.RolId, rp.PermisoId });

        builder.HasOne(rp => rp.Rol)
            .WithMany(r => r.RolPermisos)
            .HasForeignKey(rp => rp.RolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permiso)
            .WithMany(p => p.RolPermisos)
            .HasForeignKey(rp => rp.PermisoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
