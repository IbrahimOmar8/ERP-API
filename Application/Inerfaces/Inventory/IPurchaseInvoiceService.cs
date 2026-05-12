using Application.DTOs.Inventory;

namespace Application.Inerfaces.Inventory
{
    public interface IPurchaseInvoiceService
    {
        Task<List<PurchaseInvoiceDto>> GetAllAsync();
        Task<PurchaseInvoiceDto?> GetByIdAsync(Guid id);
        Task<PurchaseInvoiceDto> CreateAsync(CreatePurchaseInvoiceDto dto, Guid? userId);
    }

    public interface ISupplierService
    {
        Task<List<SupplierDto>> GetAllAsync();
        Task<SupplierDto?> GetByIdAsync(Guid id);
        Task<SupplierDto> CreateAsync(CreateSupplierDto dto);
        Task<SupplierDto?> UpdateAsync(Guid id, CreateSupplierDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
