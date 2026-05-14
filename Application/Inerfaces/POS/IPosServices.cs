using Application.DTOs.POS;

namespace Application.Inerfaces.POS
{
    public interface ICustomerService
    {
        Task<List<CustomerDto>> GetAllAsync(string? search);
        Task<CustomerDto?> GetByIdAsync(Guid id);
        Task<CustomerDto> CreateAsync(CreateCustomerDto dto);
        Task<CustomerDto?> UpdateAsync(Guid id, CreateCustomerDto dto);
        Task<bool> DeleteAsync(Guid id);
    }

    public interface ICashRegisterService
    {
        Task<List<CashRegisterDto>> GetAllAsync();
        Task<CashRegisterDto> CreateAsync(CreateCashRegisterDto dto);
        Task<CashRegisterDto?> UpdateAsync(Guid id, CreateCashRegisterDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<CashRegisterDto?> SetActiveAsync(Guid id, bool active);
    }

    public interface ICashSessionService
    {
        Task<CashSessionDto?> GetCurrentSessionAsync(Guid userId);
        Task<List<CashSessionDto>> GetAllAsync();
        Task<CashSessionDto> OpenAsync(OpenSessionDto dto, Guid userId);
        Task<CashSessionDto?> CloseAsync(Guid sessionId, CloseSessionDto dto);
        Task<CashSessionDto?> GetByIdAsync(Guid id);
    }

    public interface ISaleService
    {
        Task<List<SaleDto>> GetAllAsync(SaleFilterDto filter);
        Task<SaleDto?> GetByIdAsync(Guid id);
        Task<SaleDto> CreateAsync(CreateSaleDto dto, Guid cashierUserId);
        Task<bool> CancelAsync(Guid id);
        Task<SaleDto?> RefundAsync(Guid id, string? reason, Guid? userId);
    }
}
