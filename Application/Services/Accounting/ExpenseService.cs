using Application.DTOs.Accounting;
using Application.Inerfaces.Accounting;
using Domain.Models.Accounting;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Accounting
{
    public class ExpenseService : IExpenseService
    {
        private readonly ApplicationDbContext _context;

        public ExpenseService(ApplicationDbContext context) => _context = context;

        public async Task<List<ExpenseDto>> GetAllAsync(ExpenseFilterDto filter, CancellationToken ct = default)
        {
            var q = _context.Expenses.AsQueryable();
            if (filter.From.HasValue) q = q.Where(e => e.ExpenseDate >= filter.From.Value);
            if (filter.To.HasValue) q = q.Where(e => e.ExpenseDate < filter.To.Value);
            if (filter.Category.HasValue) q = q.Where(e => e.Category == filter.Category.Value);
            if (filter.CashSessionId.HasValue)
                q = q.Where(e => e.CashSessionId == filter.CashSessionId.Value);

            return await q
                .OrderByDescending(e => e.ExpenseDate)
                .Select(e => Map(e))
                .ToListAsync(ct);
        }

        public async Task<ExpenseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var e = await _context.Expenses.FindAsync(new object?[] { id }, ct);
            return e == null ? null : Map(e);
        }

        public async Task<ExpenseDto> CreateAsync(CreateExpenseDto dto, Guid? userId, CancellationToken ct = default)
        {
            var entity = new Expense
            {
                Title = dto.Title,
                Category = dto.Category,
                Amount = dto.Amount,
                ExpenseDate = dto.ExpenseDate ?? DateTime.UtcNow,
                PaymentMethod = dto.PaymentMethod,
                Reference = dto.Reference,
                Notes = dto.Notes,
                CashSessionId = dto.CashSessionId,
                RecordedByUserId = userId,
            };
            _context.Expenses.Add(entity);
            await _context.SaveChangesAsync(ct);
            return Map(entity);
        }

        public async Task<ExpenseDto?> UpdateAsync(Guid id, CreateExpenseDto dto, CancellationToken ct = default)
        {
            var e = await _context.Expenses.FindAsync(new object?[] { id }, ct);
            if (e == null) return null;
            e.Title = dto.Title;
            e.Category = dto.Category;
            e.Amount = dto.Amount;
            e.ExpenseDate = dto.ExpenseDate ?? e.ExpenseDate;
            e.PaymentMethod = dto.PaymentMethod;
            e.Reference = dto.Reference;
            e.Notes = dto.Notes;
            e.CashSessionId = dto.CashSessionId;
            await _context.SaveChangesAsync(ct);
            return Map(e);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var e = await _context.Expenses.FindAsync(new object?[] { id }, ct);
            if (e == null) return false;
            _context.Expenses.Remove(e);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<ExpenseSummaryDto> GetSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
        {
            var rows = await _context.Expenses
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate < to)
                .GroupBy(e => e.Category)
                .Select(g => new ExpensesByCategoryRow
                {
                    Category = g.Key,
                    Total = g.Sum(e => e.Amount),
                    Count = g.Count(),
                })
                .ToListAsync(ct);

            return new ExpenseSummaryDto
            {
                From = from,
                To = to,
                ByCategory = rows.OrderByDescending(r => r.Total).ToList(),
                Total = rows.Sum(r => r.Total),
                Count = rows.Sum(r => r.Count),
            };
        }

        private static ExpenseDto Map(Expense e) => new()
        {
            Id = e.Id,
            Title = e.Title,
            Category = e.Category,
            Amount = e.Amount,
            ExpenseDate = e.ExpenseDate,
            PaymentMethod = e.PaymentMethod,
            Reference = e.Reference,
            Notes = e.Notes,
            CashSessionId = e.CashSessionId,
            CreatedAt = e.CreatedAt,
        };
    }
}
