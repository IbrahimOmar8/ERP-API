using Application.DTOs.Inventory;

namespace Application.Inerfaces.Inventory
{
    public interface IStockTransferService
    {
        Task<List<StockTransferDto>> GetAllAsync(Guid? warehouseId = null);
        Task<StockTransferDto?> GetByIdAsync(Guid id);
        Task<StockTransferDto> CreateAsync(CreateStockTransferDto dto, Guid? userId);
        Task<bool> CompleteAsync(Guid id, Guid? userId);
        Task<bool> CancelAsync(Guid id);
    }
}
