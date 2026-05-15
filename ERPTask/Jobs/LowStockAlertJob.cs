using Application.DTOs.Notifications;
using Application.Inerfaces.Integration;
using Application.Inerfaces.Notifications;
using Domain.Models.Egypt;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERPTask.Jobs
{
    // Detects products under their min stock level, broadcasts a
    // "stock.low" event (SignalR + webhooks), and emails admins.
    public class LowStockAlertJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebhookService _webhooks;
        private readonly IEmailService _email;
        private readonly INotificationService _notifications;
        private readonly ILogger<LowStockAlertJob> _logger;

        public LowStockAlertJob(
            ApplicationDbContext context,
            IWebhookService webhooks,
            IEmailService email,
            INotificationService notifications,
            ILogger<LowStockAlertJob> logger)
        {
            _context = context;
            _webhooks = webhooks;
            _email = email;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken ct = default)
        {
            var low = await _context.StockItems
                .Include(s => s.Product)
                .Include(s => s.Warehouse)
                .Where(s => s.Product != null
                            && s.Product.IsActive
                            && s.Product.TrackStock
                            && s.Quantity <= s.Product.MinStockLevel)
                .Select(s => new
                {
                    ProductId = s.ProductId,
                    Sku = s.Product!.Sku,
                    Name = s.Product.NameAr,
                    Warehouse = s.Warehouse != null ? s.Warehouse.NameAr : "",
                    Quantity = s.Quantity,
                    MinLevel = s.Product.MinStockLevel,
                })
                .Take(200)
                .ToListAsync(ct);

            if (low.Count == 0) return;

            // Broadcast the summary
            await _webhooks.DispatchAsync("stock.low", new { count = low.Count, items = low }, ct);

            // Persistent notification for managers/admins
            await _notifications.CreateAsync(new CreateNotificationDto
            {
                Role = "Manager",
                Title = "تنبيه نقص مخزون",
                Message = $"يوجد {low.Count} صنف وصل أو تجاوز الحد الأدنى",
                Type = "stock.low",
                Link = "/stock",
                Severity = "warning",
            }, ct);

            // Email admins (skipped silently if no SMTP)
            var admins = await _context.Users
                .Include(u => u.UserRoles)!.ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive && u.Email != null
                            && u.UserRoles!.Any(ur => ur.Role != null && ur.Role.Name == "Admin"))
                .Select(u => u.Email!)
                .ToListAsync(ct);

            if (admins.Count > 0)
            {
                var html = "<h2>تنبيه نقص مخزون</h2>"
                    + $"<p>هناك {low.Count} صنف وصل للحد الأدنى:</p>"
                    + "<table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse;'>"
                    + "<thead><tr><th>SKU</th><th>الصنف</th><th>المخزن</th><th>المتاح</th><th>الحد الأدنى</th></tr></thead><tbody>"
                    + string.Concat(low.Select(x =>
                        $"<tr><td>{x.Sku}</td><td>{x.Name}</td><td>{x.Warehouse}</td><td>{x.Quantity}</td><td>{x.MinLevel}</td></tr>"))
                    + "</tbody></table>";
                foreach (var email in admins)
                {
                    try { await _email.SendAsync(email, "تنبيه نقص مخزون", html, ct); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Low-stock email to {Email} failed", email); }
                }
            }

            _logger.LogInformation("Low-stock alert dispatched for {Count} item(s)", low.Count);
        }
    }
}
