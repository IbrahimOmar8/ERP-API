using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPTask.Controllers
{
    // Cross-entity search — useful for a global "Cmd+K"-style search bar.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public SearchController(ApplicationDbContext context) => _context = context;

        public record SearchHit(string Type, string Id, string Title, string? Subtitle);

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int take = 10, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<SearchHit>());
            q = q.Trim();
            take = Math.Clamp(take, 1, 50);
            var like = $"%{q}%";

            // Run all queries in parallel — SQLite via EF Core handles
            // concurrent queries against the same context only with separate
            // connections, so we await sequentially with small Take().
            var products = await _context.Products
                .Where(p => p.IsActive
                    && (EF.Functions.Like(p.NameAr, like)
                        || EF.Functions.Like(p.NameEn ?? "", like)
                        || EF.Functions.Like(p.Sku, like)
                        || EF.Functions.Like(p.Barcode ?? "", like)))
                .Take(take)
                .Select(p => new SearchHit("product", p.Id.ToString(), p.NameAr, p.Sku))
                .ToListAsync(ct);

            var customers = await _context.Customers
                .Where(c => c.IsActive
                    && (EF.Functions.Like(c.Name, like)
                        || EF.Functions.Like(c.Phone ?? "", like)
                        || EF.Functions.Like(c.TaxRegistrationNumber ?? "", like)))
                .Take(take)
                .Select(c => new SearchHit("customer", c.Id.ToString(), c.Name, c.Phone))
                .ToListAsync(ct);

            var sales = await _context.Sales
                .Where(s => EF.Functions.Like(s.InvoiceNumber, like))
                .Take(take)
                .OrderByDescending(s => s.SaleDate)
                .Select(s => new SearchHit("sale", s.Id.ToString(), s.InvoiceNumber,
                    s.SaleDate.ToString("yyyy-MM-dd")))
                .ToListAsync(ct);

            var suppliers = await _context.Suppliers
                .Where(s => s.IsActive
                    && (EF.Functions.Like(s.Name, like)
                        || EF.Functions.Like(s.Phone ?? "", like)))
                .Take(take)
                .Select(s => new SearchHit("supplier", s.Id.ToString(), s.Name, s.Phone))
                .ToListAsync(ct);

            // Merge with products first (most common search target)
            var hits = new List<SearchHit>();
            hits.AddRange(products);
            hits.AddRange(customers);
            hits.AddRange(sales);
            hits.AddRange(suppliers);
            return Ok(hits.Take(take * 2));
        }
    }
}
