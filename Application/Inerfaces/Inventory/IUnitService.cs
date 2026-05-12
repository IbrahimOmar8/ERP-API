using Application.DTOs.Inventory;

namespace Application.Inerfaces.Inventory
{
    public interface IUnitService
    {
        Task<List<UnitDto>> GetAllAsync();
        Task<UnitDto?> GetByIdAsync(Guid id);
        Task<UnitDto> CreateAsync(CreateUnitDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
