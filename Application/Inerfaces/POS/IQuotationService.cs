using Application.DTOs.POS;
using Domain.Enums;

namespace Application.Inerfaces.POS
{
    public interface IQuotationService
    {
        Task<List<QuotationDto>> GetAllAsync(QuotationFilterDto filter, CancellationToken ct = default);
        Task<QuotationDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<QuotationDto> CreateAsync(CreateQuotationDto dto, Guid? userId, CancellationToken ct = default);
        Task<QuotationDto?> UpdateAsync(Guid id, CreateQuotationDto dto, CancellationToken ct = default);
        Task<QuotationDto?> SetStatusAsync(Guid id, QuotationStatus status, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

        // Turns an Accepted quote into a Sale and marks the quote Converted.
        Task<Guid> ConvertToSaleAsync(Guid quotationId, ConvertQuotationDto dto, Guid cashierUserId, CancellationToken ct = default);
    }
}
