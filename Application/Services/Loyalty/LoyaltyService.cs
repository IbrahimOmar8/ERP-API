using Application.DTOs.Loyalty;
using Application.Inerfaces.Loyalty;
using Domain.Enums;
using Domain.Models.Loyalty;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Loyalty
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly ApplicationDbContext _context;
        public LoyaltyService(ApplicationDbContext context) => _context = context;

        public async Task<LoyaltySettingsDto> GetSettingsAsync(CancellationToken ct = default)
        {
            var s = await _context.LoyaltySettings.FirstOrDefaultAsync(ct);
            if (s == null)
            {
                s = new LoyaltySettings();
                _context.LoyaltySettings.Add(s);
                await _context.SaveChangesAsync(ct);
            }
            return Map(s);
        }

        public async Task<LoyaltySettingsDto> UpdateSettingsAsync(LoyaltySettingsDto dto, CancellationToken ct = default)
        {
            var s = await _context.LoyaltySettings.FirstOrDefaultAsync(ct);
            if (s == null)
            {
                s = new LoyaltySettings();
                _context.LoyaltySettings.Add(s);
            }
            s.Enabled = dto.Enabled;
            s.PointValueEgp = dto.PointValueEgp;
            s.EgpPerPointEarned = dto.EgpPerPointEarned;
            s.MinRedeemPoints = dto.MinRedeemPoints;
            s.MaxRedeemPercent = Math.Clamp(dto.MaxRedeemPercent, 0, 100);
            s.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return Map(s);
        }

        public async Task<CustomerLoyaltyStatusDto?> GetCustomerStatusAsync(Guid customerId, CancellationToken ct = default)
        {
            var customer = await _context.Customers
                .Where(c => c.Id == customerId)
                .Select(c => new { c.Id, c.Name, c.LoyaltyPoints })
                .FirstOrDefaultAsync(ct);
            if (customer == null) return null;

            var settings = await _context.LoyaltySettings.FirstOrDefaultAsync(ct) ?? new LoyaltySettings();
            var recent = await _context.LoyaltyTransactions
                .Where(t => t.CustomerId == customerId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .Select(t => new LoyaltyTransactionDto
                {
                    Id = t.Id,
                    CustomerId = t.CustomerId,
                    Type = t.Type,
                    Points = t.Points,
                    BalanceAfter = t.BalanceAfter,
                    SaleId = t.SaleId,
                    Notes = t.Notes,
                    CreatedAt = t.CreatedAt,
                })
                .ToListAsync(ct);

            return new CustomerLoyaltyStatusDto
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                CurrentPoints = customer.LoyaltyPoints,
                PointsValue = customer.LoyaltyPoints * settings.PointValueEgp,
                RecentTransactions = recent,
            };
        }

        public async Task<int> AdjustPointsAsync(Guid customerId, int delta, string? notes, Guid? userId, CancellationToken ct = default)
        {
            var customer = await _context.Customers.FindAsync(new object?[] { customerId }, ct)
                ?? throw new InvalidOperationException("العميل غير موجود");
            customer.LoyaltyPoints += delta;
            if (customer.LoyaltyPoints < 0) customer.LoyaltyPoints = 0;
            _context.LoyaltyTransactions.Add(new LoyaltyTransaction
            {
                CustomerId = customerId,
                Type = LoyaltyTxType.Adjust,
                Points = delta,
                BalanceAfter = customer.LoyaltyPoints,
                Notes = notes,
                CreatedByUserId = userId,
            });
            await _context.SaveChangesAsync(ct);
            return customer.LoyaltyPoints;
        }

        private static LoyaltySettingsDto Map(LoyaltySettings s) => new()
        {
            Enabled = s.Enabled,
            PointValueEgp = s.PointValueEgp,
            EgpPerPointEarned = s.EgpPerPointEarned,
            MinRedeemPoints = s.MinRedeemPoints,
            MaxRedeemPercent = s.MaxRedeemPercent,
        };
    }
}
