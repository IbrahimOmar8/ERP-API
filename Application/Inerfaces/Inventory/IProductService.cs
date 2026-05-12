using Application.DTOs.Inventory;

namespace Application.Inerfaces.Inventory
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllAsync(ProductFilterDto filter);
        Task<ProductDto?> GetByIdAsync(Guid id);
        Task<ProductDto?> GetByBarcodeAsync(string barcode);
        Task<ProductDto> CreateAsync(CreateProductDto dto);
        Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
