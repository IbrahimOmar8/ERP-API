using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.DTOs.Egypt;
using Application.Inerfaces.Egypt;
using Domain.Enums;
using Domain.Models.Egypt;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Egypt
{
    public class EInvoiceService : IEInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _http;
        private readonly IEtaTokenService _tokens;
        private readonly EtaSettings _settings;
        private readonly ILogger<EInvoiceService> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public EInvoiceService(
            ApplicationDbContext context,
            HttpClient http,
            IEtaTokenService tokens,
            IOptions<EtaSettings> settings,
            ILogger<EInvoiceService> logger)
        {
            _context = context;
            _http = http;
            _tokens = tokens;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<EInvoiceSubmission> SubmitSaleAsync(Guid saleId, string? signedCmsBase64 = null, CancellationToken ct = default)
        {
            var sale = await _context.Sales
                .Include(s => s.Items!).ThenInclude(i => i.Product)
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == saleId, ct)
                ?? throw new InvalidOperationException("الفاتورة غير موجودة");

            var company = await _context.CompanyProfiles.FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("لم يتم ضبط بيانات الشركة");
            if (!_settings.Enabled || !company.EtaEnabled)
                throw new InvalidOperationException("تكامل ETA غير مفعل");

            var document = EtaDocumentBuilder.Build(sale, company);
            var canonical = EtaCanonicalSerializer.Serialize(document);
            var hash = ComputeSha256Hex(canonical);

            document.Signatures.Add(new EtaSignature
            {
                SignatureType = "I",
                Value = signedCmsBase64 ?? hash
            });

            var submission = new EInvoiceSubmission
            {
                SaleId = saleId,
                Status = EInvoiceStatus.Pending,
                HashKey = hash,
                RequestPayload = JsonSerializer.Serialize(document, JsonOpts),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                var token = await _tokens.GetAccessTokenAsync(company.EtaClientId!, company.EtaClientSecret!, ct);
                var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/v1.0/documentsubmissions";
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                req.Content = JsonContent.Create(new EtaSubmitRequest { Documents = { document } }, options: JsonOpts);

                submission.SubmittedAt = DateTime.UtcNow;
                using var resp = await _http.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);
                submission.ResponsePayload = body;

                if (!resp.IsSuccessStatusCode)
                {
                    submission.Status = EInvoiceStatus.Rejected;
                    submission.ErrorMessage = $"HTTP {(int)resp.StatusCode}";
                    _logger.LogWarning("ETA submission failed: {Status} {Body}", resp.StatusCode, body);
                }
                else
                {
                    var parsed = JsonSerializer.Deserialize<EtaSubmitResponse>(body, JsonOpts);
                    var accepted = parsed?.AcceptedDocuments?.FirstOrDefault();
                    var rejected = parsed?.RejectedDocuments?.FirstOrDefault();

                    if (accepted != null)
                    {
                        submission.SubmissionUuid = accepted.Uuid;
                        submission.LongId = accepted.LongId;
                        submission.HashKey = accepted.HashKey;
                        submission.Status = EInvoiceStatus.Submitted;
                        submission.ValidatedAt = DateTime.UtcNow;

                        sale.EInvoiceUuid = accepted.Uuid;
                        sale.EInvoiceLongId = accepted.LongId;
                        sale.EInvoiceStatus = EInvoiceStatus.Submitted;
                    }
                    else if (rejected != null)
                    {
                        submission.Status = EInvoiceStatus.Rejected;
                        submission.ErrorMessage = JsonSerializer.Serialize(rejected.Error, JsonOpts);
                        sale.EInvoiceStatus = EInvoiceStatus.Rejected;
                    }
                    else
                    {
                        submission.Status = EInvoiceStatus.Submitted;
                        submission.SubmissionUuid = parsed?.SubmissionId;
                    }
                }
            }
            catch (Exception ex)
            {
                submission.Status = EInvoiceStatus.Invalid;
                submission.ErrorMessage = ex.Message;
                _logger.LogError(ex, "ETA submission threw for sale {SaleId}", saleId);
            }

            _context.EInvoiceSubmissions.Add(submission);
            await _context.SaveChangesAsync(ct);
            return submission;
        }

        public async Task<EInvoiceSubmission> RefreshStatusAsync(Guid saleId, CancellationToken ct = default)
        {
            var submission = await _context.EInvoiceSubmissions
                .Where(s => s.SaleId == saleId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("لا توجد محاولة تقديم لهذه الفاتورة");

            if (string.IsNullOrEmpty(submission.SubmissionUuid))
                return submission;

            var company = await _context.CompanyProfiles.FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("لم يتم ضبط بيانات الشركة");

            var token = await _tokens.GetAccessTokenAsync(company.EtaClientId!, company.EtaClientSecret!, ct);
            var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/v1.0/documents/{submission.SubmissionUuid}/raw";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            submission.ResponsePayload = body;

            if (resp.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("status", out var statusEl))
                {
                    var status = (statusEl.GetString() ?? "").ToLowerInvariant();
                    submission.Status = status switch
                    {
                        "valid" => EInvoiceStatus.Valid,
                        "invalid" => EInvoiceStatus.Invalid,
                        "rejected" => EInvoiceStatus.Rejected,
                        "cancelled" => EInvoiceStatus.Cancelled,
                        "submitted" => EInvoiceStatus.Submitted,
                        _ => submission.Status
                    };
                    submission.ValidatedAt = DateTime.UtcNow;
                }

                var sale = await _context.Sales.FirstOrDefaultAsync(s => s.Id == saleId, ct);
                if (sale != null) sale.EInvoiceStatus = submission.Status;
            }
            else
            {
                submission.ErrorMessage = $"HTTP {(int)resp.StatusCode}";
            }

            await _context.SaveChangesAsync(ct);
            return submission;
        }

        public async Task<EInvoiceSubmission> CancelAsync(Guid saleId, string reason, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("سبب الإلغاء مطلوب");

            var submission = await _context.EInvoiceSubmissions
                .Where(s => s.SaleId == saleId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("لا توجد فاتورة مقدمة لإلغائها");

            if (string.IsNullOrEmpty(submission.SubmissionUuid))
                throw new InvalidOperationException("الفاتورة لم تُقبل من ETA بعد");

            var company = await _context.CompanyProfiles.FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("لم يتم ضبط بيانات الشركة");

            var token = await _tokens.GetAccessTokenAsync(company.EtaClientId!, company.EtaClientSecret!, ct);
            var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/v1.0/documents/state/{submission.SubmissionUuid}/state";
            using var req = new HttpRequestMessage(HttpMethod.Put, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(new { status = "cancelled", reason }, options: JsonOpts);

            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            submission.ResponsePayload = body;

            if (resp.IsSuccessStatusCode)
            {
                submission.Status = EInvoiceStatus.Cancelled;
                var sale = await _context.Sales.FirstOrDefaultAsync(s => s.Id == saleId, ct);
                if (sale != null) sale.EInvoiceStatus = EInvoiceStatus.Cancelled;
            }
            else
            {
                submission.ErrorMessage = $"HTTP {(int)resp.StatusCode}: {body}";
                throw new InvalidOperationException($"فشل إلغاء الفاتورة من ETA: {body}");
            }

            await _context.SaveChangesAsync(ct);
            return submission;
        }

        public Task<EInvoiceSubmission?> GetSubmissionAsync(Guid saleId)
            => _context.EInvoiceSubmissions
                .Where(s => s.SaleId == saleId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

        public async Task<IReadOnlyList<EInvoiceSubmission>> GetRecentAsync(int take = 50)
            => await _context.EInvoiceSubmissions
                .OrderByDescending(s => s.CreatedAt)
                .Take(Math.Clamp(take, 1, 200))
                .ToListAsync();

        private static string ComputeSha256Hex(string canonical)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            return Convert.ToHexString(bytes);
        }
    }
}
