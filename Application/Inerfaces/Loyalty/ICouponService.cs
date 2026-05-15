using Application.DTOs.Loyalty;

namespace Application.Inerfaces.Loyalty
{
    public interface ICouponService
    {
        Task<List<CouponDto>> GetAllAsync(CancellationToken ct = default);
        Task<CouponDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<CouponDto> CreateAsync(CreateCouponDto dto, CancellationToken ct = default);
        Task<CouponDto?> UpdateAsync(Guid id, CreateCouponDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<CouponValidationDto> ValidateAsync(ValidateCouponRequest request, CancellationToken ct = default);
    }

    public interface ILoyaltyService
    {
        Task<LoyaltySettingsDto> GetSettingsAsync(CancellationToken ct = default);
        Task<LoyaltySettingsDto> UpdateSettingsAsync(LoyaltySettingsDto dto, CancellationToken ct = default);
        Task<CustomerLoyaltyStatusDto?> GetCustomerStatusAsync(Guid customerId, CancellationToken ct = default);
        Task<int> AdjustPointsAsync(Guid customerId, int delta, string? notes, Guid? userId, CancellationToken ct = default);
    }
}
