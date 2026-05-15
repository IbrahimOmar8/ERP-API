using Application.DTOs.Payments;

namespace Application.Inerfaces.Payments
{
    public interface ICustomerPaymentService
    {
        Task<List<CustomerPaymentDto>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
        Task<CustomerPaymentDto> RecordAsync(CreateCustomerPaymentDto dto, Guid? userId, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<CustomerLedgerDto?> GetLedgerAsync(Guid customerId, DateTime? from, DateTime? to, CancellationToken ct = default);
    }

    public interface ISupplierPaymentService
    {
        Task<List<SupplierPaymentDto>> GetBySupplierAsync(Guid supplierId, CancellationToken ct = default);
        Task<SupplierPaymentDto> RecordAsync(CreateSupplierPaymentDto dto, Guid? userId, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<SupplierLedgerDto?> GetLedgerAsync(Guid supplierId, DateTime? from, DateTime? to, CancellationToken ct = default);
    }
}
