using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Bitacora.DTOs;

namespace GestionReservas.Application.Features.Bitacora;

public interface IAuditoriaService
{
    Task<PagedResultDto<AuditoriaDto>> GetAuditoriaAsync(
        AuditoriaQueryParameters query,
        CancellationToken cancellationToken = default);
}
