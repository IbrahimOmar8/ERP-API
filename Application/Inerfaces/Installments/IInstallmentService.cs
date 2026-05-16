using Application.DTOs.Installments;
using Domain.Enums;

namespace Application.Inerfaces.Installments
{
    public interface IInstallmentService
    {
        Task<List<InstallmentPlanDto>> GetAllAsync(Guid? customerId, InstallmentPlanStatus? status, CancellationToken ct = default);
        Task<InstallmentPlanDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<InstallmentPlanDto> CreateAsync(CreateInstallmentPlanDto dto, Guid? userId, CancellationToken ct = default);

        // Pays a single installment by id; creates a matching CustomerPayment on the customer ledger.
        Task<InstallmentPlanDto?> PayInstallmentAsync(Guid installmentId, PayInstallmentDto dto, Guid? userId, CancellationToken ct = default);

        Task<InstallmentPlanDto?> CancelAsync(Guid id, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

        // Cross-customer overdue list, useful for follow-up
        Task<List<InstallmentDto>> GetOverdueAsync(CancellationToken ct = default);
    }
}
