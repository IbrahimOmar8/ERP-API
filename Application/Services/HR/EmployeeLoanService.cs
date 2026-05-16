using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Enums;
using Domain.Models.HR;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.HR
{
    public class EmployeeLoanService : IEmployeeLoanService
    {
        private readonly ApplicationDbContext _context;
        public EmployeeLoanService(ApplicationDbContext context) => _context = context;

        public async Task<List<EmployeeLoanDto>> GetAllAsync(Guid? employeeId, EmployeeLoanStatus? status, CancellationToken ct = default)
        {
            var q = _context.EmployeeLoans.AsQueryable();
            if (employeeId.HasValue) q = q.Where(l => l.EmployeeId == employeeId.Value);
            if (status.HasValue) q = q.Where(l => l.Status == status.Value);

            var rows = await q.OrderByDescending(l => l.IssueDate).ToListAsync(ct);
            var ids = rows.Select(r => r.EmployeeId).Distinct().ToList();
            var names = await _context.Employees.Where(e => ids.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Name, ct);

            return rows.Select(l => Map(l, names.GetValueOrDefault(l.EmployeeId))).ToList();
        }

        public async Task<EmployeeLoanDto> CreateAsync(CreateEmployeeLoanDto dto, Guid? userId, CancellationToken ct = default)
        {
            if (dto.Installments < 1) dto.Installments = 1;
            var monthly = Math.Round(dto.Amount / dto.Installments, 2);

            var loan = new EmployeeLoan
            {
                EmployeeId = dto.EmployeeId,
                Amount = dto.Amount,
                Installments = dto.Installments,
                MonthlyDeduction = monthly,
                IssueDate = (dto.IssueDate ?? DateTime.UtcNow).Date,
                Status = EmployeeLoanStatus.Active,
                Reason = dto.Reason,
                Notes = dto.Notes,
                ApprovedByUserId = userId,
            };
            _context.EmployeeLoans.Add(loan);
            await _context.SaveChangesAsync(ct);

            var name = await _context.Employees.Where(e => e.Id == loan.EmployeeId).Select(e => e.Name).FirstOrDefaultAsync(ct);
            return Map(loan, name);
        }

        public async Task<EmployeeLoanDto?> CancelAsync(Guid id, CancellationToken ct = default)
        {
            var loan = await _context.EmployeeLoans.FindAsync(new object?[] { id }, ct);
            if (loan == null) return null;
            if (loan.Status == EmployeeLoanStatus.Completed)
                throw new InvalidOperationException("لا يمكن إلغاء سلفة مكتملة السداد");
            loan.Status = EmployeeLoanStatus.Cancelled;
            await _context.SaveChangesAsync(ct);
            var name = await _context.Employees.Where(e => e.Id == loan.EmployeeId).Select(e => e.Name).FirstOrDefaultAsync(ct);
            return Map(loan, name);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var loan = await _context.EmployeeLoans.FindAsync(new object?[] { id }, ct);
            if (loan == null) return false;
            if (loan.AmountRepaid > 0)
                throw new InvalidOperationException("لا يمكن حذف سلفة تم سداد جزء منها — استخدم الإلغاء");
            _context.EmployeeLoans.Remove(loan);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private static EmployeeLoanDto Map(EmployeeLoan l, string? name) => new()
        {
            Id = l.Id,
            EmployeeId = l.EmployeeId, EmployeeName = name,
            Amount = l.Amount, Installments = l.Installments,
            MonthlyDeduction = l.MonthlyDeduction,
            AmountRepaid = l.AmountRepaid,
            Remaining = Math.Max(0, l.Amount - l.AmountRepaid),
            IssueDate = l.IssueDate, CompletedDate = l.CompletedDate,
            Status = l.Status, Reason = l.Reason, Notes = l.Notes,
        };
    }
}
