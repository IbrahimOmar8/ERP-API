using Application.Inerfaces.Egypt;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERPTask.Jobs
{
    // Looks for sales with EInvoiceStatus = Pending/Invalid and retries
    // submission to ETA. Designed to be scheduled (e.g. every 10 minutes).
    public class EtaRetryJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IEInvoiceService _eta;
        private readonly ILogger<EtaRetryJob> _logger;

        public EtaRetryJob(ApplicationDbContext context, IEInvoiceService eta, ILogger<EtaRetryJob> logger)
        {
            _context = context;
            _eta = eta;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken ct = default)
        {
            var company = await _context.CompanyProfiles.FirstOrDefaultAsync(ct);
            if (company is null || !company.EtaEnabled) return;

            var stale = await _context.Sales
                .Where(s => s.Status == SaleStatus.Completed
                            && (s.EInvoiceStatus == null
                                || s.EInvoiceStatus == EInvoiceStatus.Pending
                                || s.EInvoiceStatus == EInvoiceStatus.Invalid))
                .OrderBy(s => s.SaleDate)
                .Take(20)
                .Select(s => s.Id)
                .ToListAsync(ct);

            foreach (var id in stale)
            {
                try { await _eta.SubmitSaleAsync(id, null, ct); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ETA retry failed for sale {SaleId}", id);
                }
            }
            if (stale.Count > 0)
                _logger.LogInformation("ETA retry job processed {Count} pending sale(s)", stale.Count);
        }
    }
}
