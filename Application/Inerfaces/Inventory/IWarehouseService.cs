using Application.DTOs.Inventory;

namespace Application.Inerfaces.Inventory
{
    public interface IWarehouseService
    {
        Task<List<WarehouseDto>> GetAllAsync();
        Task<WarehouseDto?> GetByIdAsync(Guid id);
        Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto);
        Task<WarehouseDto?> UpdateAsync(Guid id, CreateWarehouseDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
