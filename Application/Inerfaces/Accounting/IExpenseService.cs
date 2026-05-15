using Application.DTOs.Accounting;

namespace Application.Inerfaces.Accounting
{
    public interface IExpenseService
    {
        Task<List<ExpenseDto>> GetAllAsync(ExpenseFilterDto filter, CancellationToken ct = default);
        Task<ExpenseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<ExpenseDto> CreateAsync(CreateExpenseDto dto, Guid? userId, CancellationToken ct = default);
        Task<ExpenseDto?> UpdateAsync(Guid id, CreateExpenseDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<ExpenseSummaryDto> GetSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
    }
}
