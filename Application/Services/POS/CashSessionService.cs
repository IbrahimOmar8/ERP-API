using Application.DTOs.POS;
using Application.Inerfaces.POS;
using Domain.Enums;
using Domain.Models.POS;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.POS
{
    public class CashSessionService : ICashSessionService
    {
        private readonly ApplicationDbContext _context;

        public CashSessionService(ApplicationDbContext context) => _context = context;

        public async Task<CashSessionDto?> GetCurrentSessionAsync(Guid userId)
        {
            var session = await _context.CashSessions
                .Include(s => s.CashRegister)!
                .ThenInclude(r => r!.Warehouse)
                .FirstOrDefaultAsync(s =>
                    s.CashierUserId == userId && s.Status == CashSessionStatus.Open);

            return session == null ? null : Map(session);
        }

        public async Task<List<CashSessionDto>> GetAllAsync()
        {
            return await _context.CashSessions
                .Include(s => s.CashRegister)!
                .ThenInclude(r => r!.Warehouse)
                .OrderByDescending(s => s.OpenedAt)
                .Select(s => Map(s))
                .ToListAsync();
        }

        public async Task<CashSessionDto?> GetByIdAsync(Guid id)
        {
            var session = await _context.CashSessions
                .Include(s => s.CashRegister)!
                .ThenInclude(r => r!.Warehouse)
                .FirstOrDefaultAsync(s => s.Id == id);
            return session == null ? null : Map(session);
        }

        public async Task<CashSessionDto> OpenAsync(OpenSessionDto dto, Guid userId)
        {
            // Prevent opening if user already has an open session
            var existing = await _context.CashSessions
                .FirstOrDefaultAsync(s => s.CashierUserId == userId && s.Status == CashSessionStatus.Open);
            if (existing != null)
                throw new InvalidOperationException("لديك جلسة كاش مفتوحة بالفعل");

            var session = new CashSession
            {
                CashRegisterId = dto.CashRegisterId,
                CashierUserId = userId,
                OpeningBalance = dto.OpeningBalance,
                ExpectedBalance = dto.OpeningBalance,
                Status = CashSessionStatus.Open
            };
            _context.CashSessions.Add(session);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(session.Id))!;
        }

        public async Task<CashSessionDto?> CloseAsync(Guid sessionId, CloseSessionDto dto)
        {
            var session = await _context.CashSessions.FindAsync(sessionId);
            if (session == null || session.Status != CashSessionStatus.Open) return null;

            // Calculate totals from sales in this session
            var sales = await _context.Sales
                .Include(s => s.Payments)
                .Where(s => s.CashSessionId == sessionId && s.Status == SaleStatus.Completed)
                .ToListAsync();

            decimal totalCash = 0, totalCard = 0, totalOther = 0;
            foreach (var sale in sales)
            {
                foreach (var p in sale.Payments ?? new List<SalePayment>())
                {
                    switch (p.Method)
                    {
                        case PaymentMethod.Cash: totalCash += p.Amount; break;
                        case PaymentMethod.Card: totalCard += p.Amount; break;
                        default: totalOther += p.Amount; break;
                    }
                }
            }

            var refunds = await _context.SaleReturns
                .Where(r => r.CashSessionId == sessionId)
                .SumAsync(r => (decimal?)r.Total) ?? 0;

            session.ClosedAt = DateTime.UtcNow;
            session.ClosingBalance = dto.ClosingBalance;
            session.TotalCashSales = totalCash;
            session.TotalCardSales = totalCard;
            session.TotalOtherSales = totalOther;
            session.TotalRefunds = refunds;
            session.ExpectedBalance = session.OpeningBalance + totalCash - refunds;
            session.Difference = dto.ClosingBalance - session.ExpectedBalance;
            session.Status = CashSessionStatus.Closed;
            session.Notes = dto.Notes;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(sessionId);
        }

        private static CashSessionDto Map(CashSession s) => new()
        {
            Id = s.Id,
            CashRegisterId = s.CashRegisterId,
            CashRegisterName = s.CashRegister?.Name,
            WarehouseId = s.CashRegister?.WarehouseId ?? Guid.Empty,
            WarehouseName = s.CashRegister?.Warehouse?.NameAr,
            CashierUserId = s.CashierUserId,
            OpenedAt = s.OpenedAt,
            ClosedAt = s.ClosedAt,
            OpeningBalance = s.OpeningBalance,
            ClosingBalance = s.ClosingBalance,
            ExpectedBalance = s.ExpectedBalance,
            Difference = s.Difference,
            TotalCashSales = s.TotalCashSales,
            TotalCardSales = s.TotalCardSales,
            TotalOtherSales = s.TotalOtherSales,
            TotalRefunds = s.TotalRefunds,
            Status = s.Status
        };
    }
}
