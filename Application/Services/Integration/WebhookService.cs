using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.DTOs.Integration;
using Application.Inerfaces.Integration;
using Domain.Models.Integration;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.Integration
{
    public class WebhookService : IWebhookService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _http;
        private readonly ILogger<WebhookService> _logger;
        private readonly IRealtimeBroadcaster _realtime;
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        public WebhookService(
            ApplicationDbContext context,
            HttpClient http,
            ILogger<WebhookService> logger,
            IRealtimeBroadcaster realtime)
        {
            _context = context;
            _http = http;
            _logger = logger;
            _realtime = realtime;
            _http.Timeout = TimeSpan.FromSeconds(15);
        }

        public async Task<List<WebhookDto>> GetAllAsync(CancellationToken ct = default) =>
            await _context.WebhookSubscriptions
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => Map(w))
                .ToListAsync(ct);

        public async Task<WebhookDto> CreateAsync(CreateWebhookDto dto, Guid? userId, CancellationToken ct = default)
        {
            var secret = string.IsNullOrWhiteSpace(dto.Secret) ? GenerateSecret() : dto.Secret;
            var entity = new WebhookSubscription
            {
                Name = dto.Name,
                Url = dto.Url,
                Events = NormalizeEvents(dto.Events),
                Secret = secret,
                IsActive = dto.IsActive,
                CreatedByUserId = userId,
            };
            _context.WebhookSubscriptions.Add(entity);
            await _context.SaveChangesAsync(ct);
            return Map(entity);
        }

        public async Task<WebhookDto?> UpdateAsync(Guid id, CreateWebhookDto dto, CancellationToken ct = default)
        {
            var entity = await _context.WebhookSubscriptions.FindAsync(new object?[] { id }, ct);
            if (entity == null) return null;
            entity.Name = dto.Name;
            entity.Url = dto.Url;
            entity.Events = NormalizeEvents(dto.Events);
            if (!string.IsNullOrWhiteSpace(dto.Secret)) entity.Secret = dto.Secret;
            entity.IsActive = dto.IsActive;
            await _context.SaveChangesAsync(ct);
            return Map(entity);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _context.WebhookSubscriptions.FindAsync(new object?[] { id }, ct);
            if (entity == null) return false;
            _context.WebhookSubscriptions.Remove(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<WebhookDeliveryDto>> GetDeliveriesAsync(Guid subscriptionId, int take, CancellationToken ct = default) =>
            await _context.WebhookDeliveries
                .Where(d => d.SubscriptionId == subscriptionId)
                .OrderByDescending(d => d.CreatedAt)
                .Take(Math.Clamp(take, 1, 200))
                .Select(d => new WebhookDeliveryDto
                {
                    Id = d.Id,
                    SubscriptionId = d.SubscriptionId,
                    Event = d.Event,
                    ResponseStatus = d.ResponseStatus,
                    Error = d.Error,
                    Attempts = d.Attempts,
                    CreatedAt = d.CreatedAt,
                    DeliveredAt = d.DeliveredAt,
                })
                .ToListAsync(ct);

        public async Task DispatchAsync(string @event, object payload, CancellationToken ct = default)
        {
            // Broadcast to in-process clients (SignalR) immediately —
            // fire-and-forget, never block on a slow consumer.
            try { await _realtime.BroadcastAsync(@event, payload); }
            catch (Exception ex) { _logger.LogWarning(ex, "Realtime broadcast for {Event} failed", @event); }

            // Find every active subscription that listens for this event
            var subs = await _context.WebhookSubscriptions
                .Where(w => w.IsActive)
                .ToListAsync(ct);
            subs = subs.Where(w => MatchesEvent(w.Events, @event)).ToList();
            if (subs.Count == 0) return;

            var json = JsonSerializer.Serialize(new
            {
                @event,
                timestamp = DateTime.UtcNow,
                data = payload,
            }, JsonOpts);

            foreach (var sub in subs)
            {
                var delivery = new WebhookDelivery
                {
                    SubscriptionId = sub.Id,
                    Event = @event,
                    Payload = json,
                    Attempts = 1,
                };
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Post, sub.Url)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json"),
                    };
                    req.Headers.Add("X-Webhook-Event", @event);
                    req.Headers.Add("X-Webhook-Signature", Sign(json, sub.Secret));
                    req.Headers.UserAgent.Add(new ProductInfoHeaderValue("ErpApi", "1.0"));

                    using var resp = await _http.SendAsync(req, ct);
                    delivery.ResponseStatus = (int)resp.StatusCode;
                    delivery.ResponseBody = (await resp.Content.ReadAsStringAsync(ct))
                        .Substring(0, Math.Min(2000,
                            (await resp.Content.ReadAsStringAsync(ct)).Length));
                    delivery.DeliveredAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    delivery.Error = ex.Message;
                    _logger.LogWarning(ex, "Webhook delivery to {Url} failed", sub.Url);
                }
                _context.WebhookDeliveries.Add(delivery);
            }
            await _context.SaveChangesAsync(ct);
        }

        private static bool MatchesEvent(string subscriptionEvents, string @event)
        {
            return subscriptionEvents.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Any(e => string.Equals(e, @event, StringComparison.OrdinalIgnoreCase)
                       || e == "*"
                       || (e.EndsWith(".*") && @event.StartsWith(e[..^1], StringComparison.OrdinalIgnoreCase)));
        }

        private static string Sign(string body, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string GenerateSecret() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

        private static string NormalizeEvents(string raw) =>
            string.Join(",", raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant())
                .Where(e => e.Length > 0)
                .Distinct());

        private static WebhookDto Map(WebhookSubscription w) => new()
        {
            Id = w.Id,
            Name = w.Name,
            Url = w.Url,
            Events = w.Events,
            IsActive = w.IsActive,
            CreatedAt = w.CreatedAt,
            HasSecret = !string.IsNullOrEmpty(w.Secret),
        };
    }
}
