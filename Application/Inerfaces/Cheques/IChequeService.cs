using Application.DTOs.Cheques;

namespace Application.Inerfaces.Cheques
{
    public interface IChequeService
    {
        Task<List<ChequeDto>> GetAsync(ChequeFilterDto filter, CancellationToken ct = default);
        Task<ChequeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<ChequeDto> CreateAsync(CreateChequeDto dto, Guid? userId, CancellationToken ct = default);
        Task<ChequeDto?> UpdateAsync(Guid id, CreateChequeDto dto, CancellationToken ct = default);

        // Status transitions — each enforces the legal previous state.
        Task<ChequeDto?> DepositAsync(Guid id, CancellationToken ct = default);
        Task<ChequeDto?> ClearAsync(Guid id, Guid? userId, CancellationToken ct = default);
        Task<ChequeDto?> BounceAsync(Guid id, BounceChequeDto dto, CancellationToken ct = default);
        Task<ChequeDto?> CancelAsync(Guid id, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

        Task<ChequeStatsDto> GetStatsAsync(CancellationToken ct = default);
    }
}
