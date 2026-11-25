using GestionReservas.Application.Features.Comisiones.DTOs;

namespace GestionReservas.Application.Features.Comisiones;

public interface IComisionesService
{
    Task<IReadOnlyList<ComisionListadoDto>> ListarAsync(int departamentoId, CancellationToken cancellationToken = default);
    Task<AsignacionComisionDto> AsignarAsync(int comisionId, GuardarAsignacionComisionRequest request, CancellationToken cancellationToken = default);
    Task<AsignacionComisionDto> ReasignarAsync(int comisionId, int asignacionId, GuardarAsignacionComisionRequest request, CancellationToken cancellationToken = default);
    Task QuitarAsync(int comisionId, int asignacionId, CancellationToken cancellationToken = default);
}
