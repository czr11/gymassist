namespace GymAssist.Models;

public class AdminPaymentPageViewModel
{
    public IReadOnlyList<AdminPaymentListViewModel> Pagos { get; init; } = [];
    public int PaginaActual { get; init; }
    public int TotalPaginas { get; init; }
    public int TotalPagos { get; init; }

    public bool TienePaginaAnterior => PaginaActual > 1;
    public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
}
