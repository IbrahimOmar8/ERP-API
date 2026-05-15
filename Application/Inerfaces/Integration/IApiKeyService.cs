using Application.DTOs.Integration;
using Domain.Models.Integration;

namespace Application.Inerfaces.Integration
{
    public interface IApiKeyService
    {
        Task<List<ApiKeyDto>> GetAllAsync(CancellationToken ct = default);
        Task<CreatedApiKeyDto> CreateAsync(CreateApiKeyDto dto, Guid? userId, CancellationToken ct = default);
        Task<bool> RevokeAsync(Guid id, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

        // Used by the auth middleware to resolve an incoming key
        Task<ApiKey?> ValidateAsync(string rawKey, string? remoteIp, CancellationToken ct = default);
    }
}
