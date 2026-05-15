using Application.DTOs.Loyalty;
using Application.Inerfaces.Loyalty;
using Domain.Enums;
using Domain.Models.Loyalty;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Loyalty
{
    public class CouponService : ICouponService
    {
        private readonly ApplicationDbContext _context;
        public CouponService(ApplicationDbContext context) => _context = context;

        public async Task<List<CouponDto>> GetAllAsync(CancellationToken ct = default) =>
            await _context.Coupons
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => Map(c))
                .ToListAsync(ct);

        public async Task<CouponDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var c = await _context.Coupons.FindAsync(new object?[] { id }, ct);
            return c == null ? null : Map(c);
        }

        public async Task<CouponDto> CreateAsync(CreateCouponDto dto, CancellationToken ct = default)
        {
            var code = NormalizeCode(dto.Code);
            if (await _context.Coupons.AnyAsync(c => c.Code == code, ct))
                throw new InvalidOperationException("كود الكوبون مستخدم بالفعل");

            var entity = new Coupon
            {
                Code = code,
                Description = dto.Description,
                Type = dto.Type,
                Value = dto.Value,
                MinSubtotal = dto.MinSubtotal,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                MaxUses = dto.MaxUses,
                MaxUsesPerCustomer = dto.MaxUsesPerCustomer,
                IsActive = dto.IsActive,
            };
            _context.Coupons.Add(entity);
            await _context.SaveChangesAsync(ct);
            return Map(entity);
        }

        public async Task<CouponDto?> UpdateAsync(Guid id, CreateCouponDto dto, CancellationToken ct = default)
        {
            var entity = await _context.Coupons.FindAsync(new object?[] { id }, ct);
            if (entity == null) return null;
            var code = NormalizeCode(dto.Code);
            if (entity.Code != code && await _context.Coupons.AnyAsync(c => c.Code == code, ct))
                throw new InvalidOperationException("كود الكوبون مستخدم بالفعل");
            entity.Code = code;
            entity.Description = dto.Description;
            entity.Type = dto.Type;
            entity.Value = dto.Value;
            entity.MinSubtotal = dto.MinSubtotal;
            entity.MaxDiscountAmount = dto.MaxDiscountAmount;
            entity.ValidFrom = dto.ValidFrom;
            entity.ValidTo = dto.ValidTo;
            entity.MaxUses = dto.MaxUses;
            entity.MaxUsesPerCustomer = dto.MaxUsesPerCustomer;
            entity.IsActive = dto.IsActive;
            await _context.SaveChangesAsync(ct);
            return Map(entity);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _context.Coupons.FindAsync(new object?[] { id }, ct);
            if (entity == null) return false;
            _context.Coupons.Remove(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<CouponValidationDto> ValidateAsync(ValidateCouponRequest request, CancellationToken ct = default)
        {
            var code = NormalizeCode(request.Code);
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == code, ct);
            if (coupon == null)
                return new CouponValidationDto { Valid = false, Error = "الكوبون غير موجود" };
            if (!coupon.IsActive)
                return new CouponValidationDto { Valid = false, Error = "الكوبون غير مفعّل" };

            var now = DateTime.UtcNow;
            if (coupon.ValidFrom.HasValue && now < coupon.ValidFrom.Value)
                return new CouponValidationDto { Valid = false, Error = "الكوبون لم يبدأ بعد" };
            if (coupon.ValidTo.HasValue && now > coupon.ValidTo.Value)
                return new CouponValidationDto { Valid = false, Error = "انتهت صلاحية الكوبون" };
            if (coupon.MaxUses.HasValue && coupon.UsageCount >= coupon.MaxUses.Value)
                return new CouponValidationDto { Valid = false, Error = "استُهلك الكوبون بالكامل" };
            if (request.Subtotal < coupon.MinSubtotal)
                return new CouponValidationDto
                {
                    Valid = false,
                    Error = $"الحد الأدنى للفاتورة: {coupon.MinSubtotal:N2}",
                };

            if (coupon.MaxUsesPerCustomer.HasValue && request.CustomerId.HasValue)
            {
                var customerUses = await _context.Sales
                    .CountAsync(s => s.CouponId == coupon.Id && s.CustomerId == request.CustomerId.Value, ct);
                if (customerUses >= coupon.MaxUsesPerCustomer.Value)
                    return new CouponValidationDto { Valid = false, Error = "تجاوزت حد الاستخدام لهذا العميل" };
            }

            var discount = CalculateDiscount(coupon, request.Subtotal);
            return new CouponValidationDto
            {
                Valid = true,
                DiscountAmount = discount,
                Description = coupon.Description,
            };
        }

        public static decimal CalculateDiscount(Coupon coupon, decimal subtotal)
        {
            decimal discount = coupon.Type == DiscountType.Percentage
                ? subtotal * (coupon.Value / 100m)
                : coupon.Value;
            if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount.Value)
                discount = coupon.MaxDiscountAmount.Value;
            return Math.Min(discount, subtotal); // never exceed subtotal
        }

        private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

        private static CouponDto Map(Coupon c) => new()
        {
            Id = c.Id,
            Code = c.Code,
            Description = c.Description,
            Type = c.Type,
            Value = c.Value,
            MinSubtotal = c.MinSubtotal,
            MaxDiscountAmount = c.MaxDiscountAmount,
            ValidFrom = c.ValidFrom,
            ValidTo = c.ValidTo,
            MaxUses = c.MaxUses,
            MaxUsesPerCustomer = c.MaxUsesPerCustomer,
            UsageCount = c.UsageCount,
            IsActive = c.IsActive,
        };
    }
}
