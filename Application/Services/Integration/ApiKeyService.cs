using System.Security.Cryptography;
using Application.DTOs.Integration;
using Application.Inerfaces.Integration;
using Domain.Models.Integration;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Integration
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly ApplicationDbContext _context;
        private const string KeyPrefix = "erp_";  // help users identify our keys

        public ApiKeyService(ApplicationDbContext context) => _context = context;

        public async Task<List<ApiKeyDto>> GetAllAsync(CancellationToken ct = default) =>
            await _context.ApiKeys
                .OrderByDescending(k => k.CreatedAt)
                .Select(k => Map(k))
                .ToListAsync(ct);

        public async Task<CreatedApiKeyDto> CreateAsync(CreateApiKeyDto dto, Guid? userId, CancellationToken ct = default)
        {
            // Generate 32 random bytes → 43-char base64url (URL-safe). Prefix it so
            // logs/leaks are recognizable, e.g. "erp_AbCd...". We store only the hash.
            var raw = KeyPrefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");
            var hash = Sha256Hex(raw);

            var entity = new ApiKey
            {
                Name = dto.Name,
                Prefix = raw[..Math.Min(12, raw.Length)],
                KeyHash = hash,
                Scopes = dto.Scopes,
                ExpiresAt = dto.ExpiresAt,
                CreatedByUserId = userId,
            };
            _context.ApiKeys.Add(entity);
            await _context.SaveChangesAsync(ct);

            return new CreatedApiKeyDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Prefix = entity.Prefix,
                Scopes = entity.Scopes,
                CreatedAt = entity.CreatedAt,
                ExpiresAt = entity.ExpiresAt,
                IsActive = entity.IsActive,
                RawKey = raw,
            };
        }

        public async Task<bool> RevokeAsync(Guid id, CancellationToken ct = default)
        {
            var k = await _context.ApiKeys.FindAsync(new object?[] { id }, ct);
            if (k == null) return false;
            k.IsActive = false;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var k = await _context.ApiKeys.FindAsync(new object?[] { id }, ct);
            if (k == null) return false;
            _context.ApiKeys.Remove(k);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<ApiKey?> ValidateAsync(string rawKey, string? remoteIp, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rawKey)) return null;
            var hash = Sha256Hex(rawKey);
            var key = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.KeyHash == hash && k.IsActive, ct);
            if (key == null) return null;
            if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow) return null;

            key.LastUsedAt = DateTime.UtcNow;
            key.LastUsedIp = remoteIp;
            await _context.SaveChangesAsync(ct);
            return key;
        }

        private static string Sha256Hex(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private static ApiKeyDto Map(ApiKey k) => new()
        {
            Id = k.Id,
            Name = k.Name,
            Prefix = k.Prefix,
            Scopes = k.Scopes,
            CreatedAt = k.CreatedAt,
            ExpiresAt = k.ExpiresAt,
            LastUsedAt = k.LastUsedAt,
            IsActive = k.IsActive,
        };
    }
}
