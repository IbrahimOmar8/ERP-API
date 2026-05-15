using Application.DTOs.POS;

namespace Application.Inerfaces.POS
{
    public interface IHeldOrderService
    {
        // List orders held by this cashier (Admin/Manager can pass null to see all)
        Task<List<HeldOrderSummaryDto>> GetAllAsync(Guid? cashierUserId, CancellationToken ct = default);

        Task<HeldOrderDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<HeldOrderSummaryDto> CreateAsync(CreateHeldOrderDto dto, Guid cashierUserId, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
