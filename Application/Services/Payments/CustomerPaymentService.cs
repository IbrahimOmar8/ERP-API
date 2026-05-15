using Application.DTOs.Payments;
using Application.Inerfaces.Payments;
using Domain.Enums;
using Domain.Models.Payments;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Payments
{
    public class CustomerPaymentService : ICustomerPaymentService
    {
        private readonly ApplicationDbContext _context;
        public CustomerPaymentService(ApplicationDbContext context) => _context = context;

        public async Task<List<CustomerPaymentDto>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
            await _context.CustomerPayments
                .Where(p => p.CustomerId == customerId)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => Map(p))
                .ToListAsync(ct);

        public async Task<CustomerPaymentDto> RecordAsync(CreateCustomerPaymentDto dto, Guid? userId, CancellationToken ct = default)
        {
            var customer = await _context.Customers.FindAsync(new object?[] { dto.CustomerId }, ct)
                ?? throw new InvalidOperationException("العميل غير موجود");

            var payment = new CustomerPayment
            {
                CustomerId = dto.CustomerId,
                Amount = dto.Amount,
                Method = dto.Method,
                Reference = dto.Reference,
                Notes = dto.Notes,
                PaymentDate = dto.PaymentDate ?? DateTime.UtcNow,
                RecordedByUserId = userId,
            };
            _context.CustomerPayments.Add(payment);

            // Receiving a payment reduces what the customer owes us
            customer.Balance -= dto.Amount;

            await _context.SaveChangesAsync(ct);
            return Map(payment);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var p = await _context.CustomerPayments.FindAsync(new object?[] { id }, ct);
            if (p == null) return false;
            var customer = await _context.Customers.FindAsync(new object?[] { p.CustomerId }, ct);
            if (customer != null) customer.Balance += p.Amount; // reverse the deduction
            _context.CustomerPayments.Remove(p);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<CustomerLedgerDto?> GetLedgerAsync(Guid customerId, DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            var customer = await _context.Customers
                .Where(c => c.Id == customerId)
                .Select(c => new { c.Id, c.Name })
                .FirstOrDefaultAsync(ct);
            if (customer == null) return null;

            var f = from ?? DateTime.MinValue;
            var t = to ?? DateTime.UtcNow.AddDays(1);

            // Debits: completed sales (credit-portion = unpaid amount)
            var sales = await _context.Sales
                .Where(s => s.CustomerId == customerId
                            && s.Status == SaleStatus.Completed
                            && s.SaleDate >= f && s.SaleDate < t)
                .OrderBy(s => s.SaleDate)
                .Select(s => new
                {
                    s.SaleDate,
                    s.InvoiceNumber,
                    Owed = s.Total - s.PaidAmount,
                })
                .ToListAsync(ct);

            // Credits: customer payments received
            var payments = await _context.CustomerPayments
                .Where(p => p.CustomerId == customerId
                            && p.PaymentDate >= f && p.PaymentDate < t)
                .OrderBy(p => p.PaymentDate)
                .Select(p => new
                {
                    p.PaymentDate,
                    p.Amount,
                    p.Method,
                    p.Reference,
                    p.Notes,
                })
                .ToListAsync(ct);

            // Opening balance — sum before "from"
            var openingSalesOwed = await _context.Sales
                .Where(s => s.CustomerId == customerId
                            && s.Status == SaleStatus.Completed
                            && s.SaleDate < f)
                .SumAsync(s => (decimal?)(s.Total - s.PaidAmount), ct) ?? 0m;
            var openingPayments = await _context.CustomerPayments
                .Where(p => p.CustomerId == customerId && p.PaymentDate < f)
                .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
            var opening = openingSalesOwed - openingPayments;

            // Merge sorted
            var rows = new List<LedgerRow>();
            int si = 0, pi = 0;
            decimal balance = opening;
            while (si < sales.Count || pi < payments.Count)
            {
                bool takeSale = si < sales.Count && (pi >= payments.Count || sales[si].SaleDate <= payments[pi].PaymentDate);
                if (takeSale)
                {
                    var s = sales[si++];
                    balance += s.Owed;
                    rows.Add(new LedgerRow
                    {
                        Date = s.SaleDate,
                        Type = "sale",
                        Reference = s.InvoiceNumber,
                        Debit = s.Owed,
                        Balance = balance,
                    });
                }
                else
                {
                    var p = payments[pi++];
                    balance -= p.Amount;
                    rows.Add(new LedgerRow
                    {
                        Date = p.PaymentDate,
                        Type = "payment",
                        Reference = p.Reference ?? "—",
                        Credit = p.Amount,
                        Balance = balance,
                        Notes = p.Notes,
                    });
                }
            }

            return new CustomerLedgerDto
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                OpeningBalance = opening,
                ClosingBalance = balance,
                TotalDebit = rows.Sum(r => r.Debit),
                TotalCredit = rows.Sum(r => r.Credit),
                Rows = rows,
            };
        }

        private static CustomerPaymentDto Map(CustomerPayment p) => new()
        {
            Id = p.Id,
            CustomerId = p.CustomerId,
            Amount = p.Amount,
            Method = p.Method,
            Reference = p.Reference,
            Notes = p.Notes,
            PaymentDate = p.PaymentDate,
        };
    }
}
