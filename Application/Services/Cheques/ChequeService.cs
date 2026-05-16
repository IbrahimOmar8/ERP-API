using Application.DTOs.Cheques;
using Application.Inerfaces.Cheques;
using Domain.Enums;
using Domain.Models.Cheques;
using Domain.Models.Payments;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Cheques
{
    public class ChequeService : IChequeService
    {
        private readonly ApplicationDbContext _context;
        public ChequeService(ApplicationDbContext context) => _context = context;

        public async Task<List<ChequeDto>> GetAsync(ChequeFilterDto filter, CancellationToken ct = default)
        {
            var q = _context.Cheques.AsQueryable();
            if (filter.Type.HasValue) q = q.Where(c => c.Type == filter.Type.Value);
            if (filter.Status.HasValue) q = q.Where(c => c.Status == filter.Status.Value);
            if (filter.CustomerId.HasValue) q = q.Where(c => c.CustomerId == filter.CustomerId.Value);
            if (filter.SupplierId.HasValue) q = q.Where(c => c.SupplierId == filter.SupplierId.Value);
            if (filter.DueFrom.HasValue) q = q.Where(c => c.DueDate >= filter.DueFrom.Value);
            if (filter.DueTo.HasValue) q = q.Where(c => c.DueDate <= filter.DueTo.Value);

            var rows = await q.OrderBy(c => c.DueDate).ToListAsync(ct);
            return await MapManyAsync(rows, ct);
        }

        public async Task<ChequeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var c = await _context.Cheques.FindAsync(new object?[] { id }, ct);
            if (c == null) return null;
            return (await MapManyAsync(new[] { c }, ct)).FirstOrDefault();
        }

        public async Task<ChequeDto> CreateAsync(CreateChequeDto dto, Guid? userId, CancellationToken ct = default)
        {
            ValidateCounterparty(dto.Type, dto.CustomerId, dto.SupplierId);

            var c = new Cheque
            {
                ChequeNumber = dto.ChequeNumber.Trim(),
                BankName = dto.BankName.Trim(),
                BranchName = dto.BranchName,
                AccountHolderName = dto.AccountHolderName,
                Amount = dto.Amount,
                IssueDate = dto.IssueDate.Date,
                DueDate = dto.DueDate.Date,
                Type = dto.Type,
                CustomerId = dto.Type == ChequeType.Incoming ? dto.CustomerId : null,
                SupplierId = dto.Type == ChequeType.Outgoing ? dto.SupplierId : null,
                SaleId = dto.SaleId,
                PurchaseInvoiceId = dto.PurchaseInvoiceId,
                Notes = dto.Notes,
                CreatedByUserId = userId,
                Status = ChequeStatus.Pending,
            };
            _context.Cheques.Add(c);
            await _context.SaveChangesAsync(ct);
            return (await GetByIdAsync(c.Id, ct))!;
        }

        public async Task<ChequeDto?> UpdateAsync(Guid id, CreateChequeDto dto, CancellationToken ct = default)
        {
            var c = await _context.Cheques.FindAsync(new object?[] { id }, ct);
            if (c == null) return null;
            if (c.Status != ChequeStatus.Pending)
                throw new InvalidOperationException("لا يمكن تعديل شيك بعد إيداعه أو تحصيله");

            ValidateCounterparty(dto.Type, dto.CustomerId, dto.SupplierId);

            c.ChequeNumber = dto.ChequeNumber.Trim();
            c.BankName = dto.BankName.Trim();
            c.BranchName = dto.BranchName;
            c.AccountHolderName = dto.AccountHolderName;
            c.Amount = dto.Amount;
            c.IssueDate = dto.IssueDate.Date;
            c.DueDate = dto.DueDate.Date;
            c.Type = dto.Type;
            c.CustomerId = dto.Type == ChequeType.Incoming ? dto.CustomerId : null;
            c.SupplierId = dto.Type == ChequeType.Outgoing ? dto.SupplierId : null;
            c.SaleId = dto.SaleId;
            c.PurchaseInvoiceId = dto.PurchaseInvoiceId;
            c.Notes = dto.Notes;
            c.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<ChequeDto?> DepositAsync(Guid id, CancellationToken ct = default)
        {
            var c = await _context.Cheques.FindAsync(new object?[] { id }, ct);
            if (c == null) return null;
            if (c.Status != ChequeStatus.Pending && c.Status != ChequeStatus.Bounced)
                throw new InvalidOperationException("لا يمكن إيداع الشيك في حالته الحالية");

            c.Status = ChequeStatus.Deposited;
            c.DepositedAt = DateTime.UtcNow;
            c.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<ChequeDto?> ClearAsync(Guid id, Guid? userId, CancellationToken ct = default)
        {
            var c = await _context.Cheques.FindAsync(new object?[] { id }, ct);
            if (c == null) return null;
            if (c.Status != ChequeStatus.Deposited && c.Status != ChequeStatus.Pending)
                throw new InvalidOperationException("لا يمكن تحصيل الشيك في حالته الحالية");

            // Auto-create the matching payment so customer/supplier balance is updated.
            if (c.Type == ChequeType.Incoming && c.CustomerId.HasValue)
            {
                var payment = new CustomerPayment
                {
                    CustomerId = c.CustomerId.Value,
                    Amount = c.Amount,
                    Method = PaymentMethod.Cheque,
                    Reference = $"Cheque #{c.ChequeNumber} — {c.BankName}",
                    Notes = c.Notes,
                    PaymentDate = DateTime.UtcNow,
                    RecordedByUserId = userId,
                };
                _context.CustomerPayments.Add(payment);
                c.LinkedPaymentId = payment.Id;
            }
            else if (c.Type == ChequeType.Outgoing && c.SupplierId.HasValue)
            {
                var payment = new SupplierPayment
                {
                    SupplierId = c.SupplierId.Value,
                    Amount = c.Amount,
                    Method = PaymentMethod.Cheque,
                    Reference = $"Cheque #{c.ChequeNumber} — {c.BankName}",
                    Notes = c.Notes,
                    PaymentDate = DateTime.UtcNow,
                    RecordedByUserId = userId,
                };
                _context.SupplierPayments.Add(payment);
                c.LinkedPaymentId = payment.Id;
            }

            c.Status = ChequeStatus.Cleared;
            c.ClearedAt = DateTime.UtcNow;
            c.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<ChequeDto?> BounceAsync(Guid id, BounceChequeDto dto, CancellationToken ct = default)
        {
            var c = await _context.Cheques.FindAsync(new object?[] { id }, ct);
            if (c == null) return null;
            if (c.Status != ChequeStatus.Deposited)
                throw new InvalidOperationException("لا يرتد إلا الشيك المودع");

            c.Status = ChequeStatus.Bounced;
            c.BouncedAt = DateTime.UtcNow;
            c.BounceReason = dto.Reason;
            c.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<ChequeDto?> CancelAsync(Guid id, CancellationToken ct = default)
        {
            var c = await _context.Cheques.FindAsync(new object?[] { id }, ct);
            if (c == null) return null;
            if (c.Status == ChequeStatus.Cleared)
                throw new InvalidOperationException("لا يمكن إلغاء شيك تم تحصيله");
            c.Status = ChequeStatus.Cancelled;
            c.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var c = await _context.Cheques.FindAsync(new object?[] { id }, ct);
            if (c == null) return false;
            if (c.Status == ChequeStatus.Cleared)
                throw new InvalidOperationException("لا يمكن حذف شيك تم تحصيله — استخدم الإلغاء");
            _context.Cheques.Remove(c);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<ChequeStatsDto> GetStatsAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;
            var nextWeek = today.AddDays(7);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var active = await _context.Cheques
                .Where(c => c.Status == ChequeStatus.Pending || c.Status == ChequeStatus.Deposited)
                .Select(c => new { c.Type, c.Amount, c.DueDate })
                .ToListAsync(ct);

            var bounced = await _context.Cheques.CountAsync(c => c.Status == ChequeStatus.Bounced && c.BouncedAt >= monthStart, ct);

            return new ChequeStatsDto
            {
                IncomingPending = active.Count(c => c.Type == ChequeType.Incoming),
                IncomingPendingAmount = active.Where(c => c.Type == ChequeType.Incoming).Sum(c => c.Amount),
                OutgoingPending = active.Count(c => c.Type == ChequeType.Outgoing),
                OutgoingPendingAmount = active.Where(c => c.Type == ChequeType.Outgoing).Sum(c => c.Amount),
                DueSoon = active.Count(c => c.DueDate >= today && c.DueDate <= nextWeek),
                Overdue = active.Count(c => c.DueDate < today),
                BouncedThisMonth = bounced,
            };
        }

        // ─── helpers ────────────────────────────────────────────────────

        private static void ValidateCounterparty(ChequeType type, Guid? customerId, Guid? supplierId)
        {
            if (type == ChequeType.Incoming && (customerId == null || customerId == Guid.Empty))
                throw new InvalidOperationException("اختر العميل للشيك الوارد");
            if (type == ChequeType.Outgoing && (supplierId == null || supplierId == Guid.Empty))
                throw new InvalidOperationException("اختر المورد للشيك الصادر");
        }

        private async Task<List<ChequeDto>> MapManyAsync(IEnumerable<Cheque> rows, CancellationToken ct)
        {
            var custIds = rows.Where(r => r.CustomerId.HasValue).Select(r => r.CustomerId!.Value).Distinct().ToList();
            var supIds = rows.Where(r => r.SupplierId.HasValue).Select(r => r.SupplierId!.Value).Distinct().ToList();
            var saleIds = rows.Where(r => r.SaleId.HasValue).Select(r => r.SaleId!.Value).Distinct().ToList();
            var purIds = rows.Where(r => r.PurchaseInvoiceId.HasValue).Select(r => r.PurchaseInvoiceId!.Value).Distinct().ToList();

            var custs = custIds.Count == 0 ? new Dictionary<Guid, string>()
                : await _context.Customers.Where(x => custIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
            var sups = supIds.Count == 0 ? new Dictionary<Guid, string>()
                : await _context.Suppliers.Where(x => supIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
            var sales = saleIds.Count == 0 ? new Dictionary<Guid, string>()
                : await _context.Sales.Where(x => saleIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.InvoiceNumber, ct);
            var purs = purIds.Count == 0 ? new Dictionary<Guid, string>()
                : await _context.PurchaseInvoices.Where(x => purIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.InvoiceNumber, ct);

            var today = DateTime.UtcNow.Date;
            return rows.Select(c => new ChequeDto
            {
                Id = c.Id,
                ChequeNumber = c.ChequeNumber, BankName = c.BankName, BranchName = c.BranchName,
                AccountHolderName = c.AccountHolderName, Amount = c.Amount,
                IssueDate = c.IssueDate, DueDate = c.DueDate,
                Type = c.Type, Status = c.Status,
                CustomerId = c.CustomerId,
                CustomerName = c.CustomerId.HasValue ? custs.GetValueOrDefault(c.CustomerId.Value) : null,
                SupplierId = c.SupplierId,
                SupplierName = c.SupplierId.HasValue ? sups.GetValueOrDefault(c.SupplierId.Value) : null,
                SaleId = c.SaleId,
                SaleNumber = c.SaleId.HasValue ? sales.GetValueOrDefault(c.SaleId.Value) : null,
                PurchaseInvoiceId = c.PurchaseInvoiceId,
                PurchaseNumber = c.PurchaseInvoiceId.HasValue ? purs.GetValueOrDefault(c.PurchaseInvoiceId.Value) : null,
                DepositedAt = c.DepositedAt, ClearedAt = c.ClearedAt, BouncedAt = c.BouncedAt,
                BounceReason = c.BounceReason, Notes = c.Notes,
                LinkedPaymentId = c.LinkedPaymentId,
                DaysToDue = (int)(c.DueDate.Date - today).TotalDays,
            }).ToList();
        }
    }
}
