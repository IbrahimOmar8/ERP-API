using Application.DTOs.Production;

namespace Application.Inerfaces.Production
{
    public interface IBomService
    {
        Task<List<BomDto>> GetAllAsync(Guid? productId = null, CancellationToken ct = default);
        Task<BomDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<BomDto> CreateAsync(CreateBomDto dto, CancellationToken ct = default);
        Task<BomDto?> UpdateAsync(Guid id, CreateBomDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public interface IProductionOrderService
    {
        Task<List<ProductionOrderDto>> GetAllAsync(CancellationToken ct = default);
        Task<ProductionOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<ProductionOrderDto> CreateAsync(CreateProductionOrderDto dto, Guid? userId, CancellationToken ct = default);

        // Atomically deducts components from stock and adds finished product stock.
        // Throws if any component is short on stock.
        Task<ProductionOrderDto?> CompleteAsync(Guid id, Guid? userId, CancellationToken ct = default);

        Task<ProductionOrderDto?> CancelAsync(Guid id, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
