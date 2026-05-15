using Application.DTOs.Reports;

namespace Application.Inerfaces.Reports
{
    public interface IReportService
    {
        Task<DashboardKpiDto> GetDashboardAsync(CancellationToken ct = default);
        Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to, Guid? warehouseId, CancellationToken ct = default);
        Task<IReadOnlyList<TopProductRow>> GetTopProductsAsync(DateTime from, DateTime to, int take, CancellationToken ct = default);
        Task<IReadOnlyList<TopCustomerRow>> GetTopCustomersAsync(DateTime from, DateTime to, int take, CancellationToken ct = default);
        Task<IReadOnlyList<StockReportRow>> GetStockReportAsync(Guid? warehouseId, bool onlyLow, CancellationToken ct = default);
        Task<CashSessionReportDto?> GetCashSessionReportAsync(Guid sessionId, CancellationToken ct = default);
    }
}
