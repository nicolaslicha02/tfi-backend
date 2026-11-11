namespace GestionReservas.Application.Common.Models;

/// <summary>
/// Envoltorio genérico para resultados paginados de consultas.
/// </summary>
/// <typeparam name="T">Tipo de elemento en la página.</typeparam>
public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public PagedResultDto() { }

    public PagedResultDto(IReadOnlyList<T> items, int total, int page, int pageSize)
    {
        Items = items;
        Total = total;
        Page = page;
        PageSize = pageSize;
    }
}
