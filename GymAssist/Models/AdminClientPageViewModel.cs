namespace GymAssist.Models;

public class AdminClientPageViewModel
{
    public IReadOnlyList<Cliente> Clientes { get; init; } = [];
    public int PaginaActual { get; init; }
    public int TotalPaginas { get; init; }
    public int TotalClientes { get; init; }

    public bool TienePaginaAnterior => PaginaActual > 1;
    public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
}
