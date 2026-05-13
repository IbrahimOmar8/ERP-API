using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using Application.Inerfaces.Egypt;
using Microsoft.Extensions.Options;

namespace Application.Services.Egypt
{
    public class EtaTokenService : IEtaTokenService
    {
        private readonly HttpClient _http;
        private readonly EtaSettings _settings;
        private readonly ConcurrentDictionary<string, CachedToken> _cache = new();
        private static readonly SemaphoreSlim _gate = new(1, 1);

        public EtaTokenService(HttpClient http, IOptions<EtaSettings> options)
        {
            _http = http;
            _settings = options.Value;
        }

        public async Task<string> GetAccessTokenAsync(string clientId, string clientSecret, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("بيانات اعتماد ETA غير مكتملة");

            if (_cache.TryGetValue(clientId, out var existing) && existing.ExpiresAt > DateTime.UtcNow.AddSeconds(30))
                return existing.AccessToken;

            await _gate.WaitAsync(ct);
            try
            {
                if (_cache.TryGetValue(clientId, out existing) && existing.ExpiresAt > DateTime.UtcNow.AddSeconds(30))
                    return existing.AccessToken;

                using var req = new HttpRequestMessage(HttpMethod.Post, _settings.AuthUrl)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["scope"] = _settings.Scope
                    })
                };

                using var resp = await _http.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode)
                    throw new InvalidOperationException($"فشل الحصول على رمز ETA: {(int)resp.StatusCode} {body}");

                using var doc = JsonDocument.Parse(body);
                var token = doc.RootElement.GetProperty("access_token").GetString()
                    ?? throw new InvalidOperationException("استجابة ETA لا تحتوي على access_token");
                var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;

                _cache[clientId] = new CachedToken(token, DateTime.UtcNow.AddSeconds(expiresIn));
                return token;
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Invalidate(string clientId) => _cache.TryRemove(clientId, out _);

        private record CachedToken(string AccessToken, DateTime ExpiresAt);
    }
}
