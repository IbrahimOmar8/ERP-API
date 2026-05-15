using Application.DTOs.Payments;
using Application.Inerfaces.Payments;
using Domain.Models.Payments;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Payments
{
    public class SupplierPaymentService : ISupplierPaymentService
    {
        private readonly ApplicationDbContext _context;
        public SupplierPaymentService(ApplicationDbContext context) => _context = context;

        public async Task<List<SupplierPaymentDto>> GetBySupplierAsync(Guid supplierId, CancellationToken ct = default) =>
            await _context.SupplierPayments
                .Where(p => p.SupplierId == supplierId)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => Map(p))
                .ToListAsync(ct);

        public async Task<SupplierPaymentDto> RecordAsync(CreateSupplierPaymentDto dto, Guid? userId, CancellationToken ct = default)
        {
            var supplier = await _context.Suppliers.FindAsync(new object?[] { dto.SupplierId }, ct)
                ?? throw new InvalidOperationException("المورد غير موجود");

            var payment = new SupplierPayment
            {
                SupplierId = dto.SupplierId,
                Amount = dto.Amount,
                Method = dto.Method,
                Reference = dto.Reference,
                Notes = dto.Notes,
                PaymentDate = dto.PaymentDate ?? DateTime.UtcNow,
                RecordedByUserId = userId,
            };
            _context.SupplierPayments.Add(payment);

            // Paying the supplier reduces what we owe them
            supplier.Balance -= dto.Amount;

            await _context.SaveChangesAsync(ct);
            return Map(payment);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var p = await _context.SupplierPayments.FindAsync(new object?[] { id }, ct);
            if (p == null) return false;
            var s = await _context.Suppliers.FindAsync(new object?[] { p.SupplierId }, ct);
            if (s != null) s.Balance += p.Amount;
            _context.SupplierPayments.Remove(p);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<SupplierLedgerDto?> GetLedgerAsync(Guid supplierId, DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            var supplier = await _context.Suppliers
                .Where(s => s.Id == supplierId)
                .Select(s => new { s.Id, s.Name })
                .FirstOrDefaultAsync(ct);
            if (supplier == null) return null;

            var f = from ?? DateTime.MinValue;
            var t = to ?? DateTime.UtcNow.AddDays(1);

            var purchases = await _context.PurchaseInvoices
                .Where(p => p.SupplierId == supplierId
                            && p.InvoiceDate >= f && p.InvoiceDate < t)
                .OrderBy(p => p.InvoiceDate)
                .Select(p => new { p.InvoiceDate, p.InvoiceNumber, Owed = p.Total - p.Paid })
                .ToListAsync(ct);

            var payments = await _context.SupplierPayments
                .Where(p => p.SupplierId == supplierId
                            && p.PaymentDate >= f && p.PaymentDate < t)
                .OrderBy(p => p.PaymentDate)
                .Select(p => new { p.PaymentDate, p.Amount, p.Method, p.Reference, p.Notes })
                .ToListAsync(ct);

            var openingOwed = await _context.PurchaseInvoices
                .Where(p => p.SupplierId == supplierId && p.InvoiceDate < f)
                .SumAsync(p => (decimal?)(p.Total - p.Paid), ct) ?? 0m;
            var openingPaid = await _context.SupplierPayments
                .Where(p => p.SupplierId == supplierId && p.PaymentDate < f)
                .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
            var opening = openingOwed - openingPaid;

            var rows = new List<LedgerRow>();
            int pi = 0, payi = 0;
            decimal balance = opening;
            while (pi < purchases.Count || payi < payments.Count)
            {
                bool takePurchase = pi < purchases.Count
                    && (payi >= payments.Count || purchases[pi].InvoiceDate <= payments[payi].PaymentDate);
                if (takePurchase)
                {
                    var p = purchases[pi++];
                    balance += p.Owed;
                    rows.Add(new LedgerRow
                    {
                        Date = p.InvoiceDate,
                        Type = "purchase",
                        Reference = p.InvoiceNumber,
                        Debit = p.Owed,
                        Balance = balance,
                    });
                }
                else
                {
                    var pay = payments[payi++];
                    balance -= pay.Amount;
                    rows.Add(new LedgerRow
                    {
                        Date = pay.PaymentDate,
                        Type = "payment",
                        Reference = pay.Reference ?? "—",
                        Credit = pay.Amount,
                        Balance = balance,
                        Notes = pay.Notes,
                    });
                }
            }

            return new SupplierLedgerDto
            {
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                OpeningBalance = opening,
                ClosingBalance = balance,
                TotalDebit = rows.Sum(r => r.Debit),
                TotalCredit = rows.Sum(r => r.Credit),
                Rows = rows,
            };
        }

        private static SupplierPaymentDto Map(SupplierPayment p) => new()
        {
            Id = p.Id,
            SupplierId = p.SupplierId,
            Amount = p.Amount,
            Method = p.Method,
            Reference = p.Reference,
            Notes = p.Notes,
            PaymentDate = p.PaymentDate,
        };
    }
}
