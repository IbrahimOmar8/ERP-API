using Application.DTOs.POS;
using Application.Inerfaces.POS;
using Domain.Enums;
using Domain.Models.POS;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.POS
{
    public class QuotationService : IQuotationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISaleService _saleService;

        public QuotationService(ApplicationDbContext context, ISaleService saleService)
        {
            _context = context;
            _saleService = saleService;
        }

        public async Task<List<QuotationDto>> GetAllAsync(QuotationFilterDto filter, CancellationToken ct = default)
        {
            var q = _context.Quotations
                .Include(x => x.Customer)
                .Include(x => x.Items)
                .AsQueryable();

            if (filter.Status.HasValue) q = q.Where(x => x.Status == filter.Status.Value);
            if (filter.CustomerId.HasValue) q = q.Where(x => x.CustomerId == filter.CustomerId.Value);
            if (filter.From.HasValue) q = q.Where(x => x.IssueDate >= filter.From.Value);
            if (filter.To.HasValue) q = q.Where(x => x.IssueDate < filter.To.Value);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                q = q.Where(x => x.QuotationNumber.Contains(s)
                              || (x.CustomerNameSnapshot != null && x.CustomerNameSnapshot.Contains(s))
                              || (x.Customer != null && x.Customer.Name.Contains(s)));
            }

            return await q.OrderByDescending(x => x.IssueDate)
                .Select(x => Map(x, includeItems: false))
                .ToListAsync(ct);
        }

        public async Task<QuotationDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var q = await _context.Quotations
                .Include(x => x.Customer)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            return q == null ? null : Map(q);
        }

        public async Task<QuotationDto> CreateAsync(CreateQuotationDto dto, Guid? userId, CancellationToken ct = default)
        {
            if (dto.Items.Count == 0)
                throw new InvalidOperationException("لا يمكن إنشاء عرض سعر بدون أصناف");

            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

            var quotation = new Quotation
            {
                QuotationNumber = await NextNumberAsync(ct),
                CustomerId = dto.CustomerId,
                CustomerNameSnapshot = dto.CustomerNameSnapshot,
                CustomerPhoneSnapshot = dto.CustomerPhoneSnapshot,
                WarehouseId = dto.WarehouseId,
                ValidUntil = dto.ValidUntil,
                DiscountPercent = dto.DiscountPercent,
                Notes = dto.Notes,
                Terms = dto.Terms,
                CreatedByUserId = userId,
                Status = QuotationStatus.Draft,
                Items = new List<QuotationItem>(),
            };

            decimal subTotal = 0, totalVat = 0;
            foreach (var i in dto.Items)
            {
                if (!products.TryGetValue(i.ProductId, out var p))
                    throw new InvalidOperationException($"المنتج {i.ProductId} غير موجود");

                var unitPrice = i.UnitPrice ?? p.SalePrice;
                var lineBefore = i.Quantity * unitPrice;
                var lineDiscount = i.DiscountAmount + (lineBefore * i.DiscountPercent / 100m);
                var lineSub = lineBefore - lineDiscount;
                var vat = lineSub * (p.VatRate / 100m);
                var lineTotal = lineSub + vat;

                quotation.Items.Add(new QuotationItem
                {
                    ProductId = p.Id,
                    ProductNameSnapshot = p.NameAr,
                    Quantity = i.Quantity,
                    UnitPrice = unitPrice,
                    DiscountAmount = lineDiscount,
                    DiscountPercent = i.DiscountPercent,
                    VatRate = p.VatRate,
                    VatAmount = vat,
                    LineSubTotal = lineSub,
                    LineTotal = lineTotal,
                });
                subTotal += lineBefore;
                totalVat += vat;
            }

            var invoiceDiscount = dto.DiscountAmount + (subTotal * dto.DiscountPercent / 100m);
            quotation.SubTotal = subTotal;
            quotation.DiscountAmount = invoiceDiscount + quotation.Items.Sum(x => x.DiscountAmount);
            quotation.VatAmount = totalVat;
            quotation.Total = subTotal
                - quotation.Items.Sum(x => x.DiscountAmount)
                - invoiceDiscount
                + totalVat;

            _context.Quotations.Add(quotation);
            await _context.SaveChangesAsync(ct);
            return (await GetByIdAsync(quotation.Id, ct))!;
        }

        public async Task<QuotationDto?> UpdateAsync(Guid id, CreateQuotationDto dto, CancellationToken ct = default)
        {
            var existing = await _context.Quotations
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.Id == id, ct);
            if (existing == null) return null;
            if (existing.Status == QuotationStatus.Converted)
                throw new InvalidOperationException("لا يمكن تعديل عرض سعر تم تحويله لفاتورة");

            // Easiest: clear items + rebuild via CreateAsync logic on the same record
            _context.QuotationItems.RemoveRange(existing.Items ?? Enumerable.Empty<QuotationItem>());
            await _context.SaveChangesAsync(ct);

            existing.CustomerId = dto.CustomerId;
            existing.CustomerNameSnapshot = dto.CustomerNameSnapshot;
            existing.CustomerPhoneSnapshot = dto.CustomerPhoneSnapshot;
            existing.WarehouseId = dto.WarehouseId;
            existing.ValidUntil = dto.ValidUntil;
            existing.DiscountPercent = dto.DiscountPercent;
            existing.Notes = dto.Notes;
            existing.Terms = dto.Terms;

            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

            existing.Items = new List<QuotationItem>();
            decimal subTotal = 0, totalVat = 0;
            foreach (var i in dto.Items)
            {
                if (!products.TryGetValue(i.ProductId, out var p))
                    throw new InvalidOperationException($"المنتج {i.ProductId} غير موجود");
                var unitPrice = i.UnitPrice ?? p.SalePrice;
                var lineBefore = i.Quantity * unitPrice;
                var lineDiscount = i.DiscountAmount + (lineBefore * i.DiscountPercent / 100m);
                var lineSub = lineBefore - lineDiscount;
                var vat = lineSub * (p.VatRate / 100m);
                var lineTotal = lineSub + vat;
                existing.Items.Add(new QuotationItem
                {
                    QuotationId = existing.Id,
                    ProductId = p.Id,
                    ProductNameSnapshot = p.NameAr,
                    Quantity = i.Quantity,
                    UnitPrice = unitPrice,
                    DiscountAmount = lineDiscount,
                    DiscountPercent = i.DiscountPercent,
                    VatRate = p.VatRate,
                    VatAmount = vat,
                    LineSubTotal = lineSub,
                    LineTotal = lineTotal,
                });
                subTotal += lineBefore;
                totalVat += vat;
            }

            var invoiceDiscount = dto.DiscountAmount + (subTotal * dto.DiscountPercent / 100m);
            existing.SubTotal = subTotal;
            existing.DiscountAmount = invoiceDiscount + existing.Items.Sum(x => x.DiscountAmount);
            existing.VatAmount = totalVat;
            existing.Total = subTotal
                - existing.Items.Sum(x => x.DiscountAmount)
                - invoiceDiscount
                + totalVat;

            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<QuotationDto?> SetStatusAsync(Guid id, QuotationStatus status, CancellationToken ct = default)
        {
            var q = await _context.Quotations.FindAsync(new object?[] { id }, ct);
            if (q == null) return null;
            if (q.Status == QuotationStatus.Converted)
                throw new InvalidOperationException("لا يمكن تغيير حالة عرض سعر مُحوّل");
            q.Status = status;
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var q = await _context.Quotations.FindAsync(new object?[] { id }, ct);
            if (q == null) return false;
            if (q.Status == QuotationStatus.Converted)
                throw new InvalidOperationException("لا يمكن حذف عرض سعر مُحوّل");
            _context.Quotations.Remove(q);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<Guid> ConvertToSaleAsync(Guid quotationId, ConvertQuotationDto dto, Guid cashierUserId, CancellationToken ct = default)
        {
            var quotation = await _context.Quotations
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.Id == quotationId, ct)
                ?? throw new InvalidOperationException("عرض السعر غير موجود");
            if (quotation.Status == QuotationStatus.Converted)
                throw new InvalidOperationException("عرض السعر مُحوّل مسبقاً");
            if (quotation.Items == null || quotation.Items.Count == 0)
                throw new InvalidOperationException("عرض السعر بدون أصناف");

            // Resolve warehouse: prefer quotation's, else fall back to session's
            Guid warehouseId;
            if (quotation.WarehouseId.HasValue) warehouseId = quotation.WarehouseId.Value;
            else
            {
                var session = await _context.CashSessions
                    .Include(s => s.CashRegister)
                    .FirstOrDefaultAsync(s => s.Id == dto.CashSessionId, ct)
                    ?? throw new InvalidOperationException("جلسة الكاش غير موجودة");
                warehouseId = session.CashRegister?.WarehouseId
                    ?? throw new InvalidOperationException("لم يتم تحديد مخزن");
            }

            var createDto = new CreateSaleDto
            {
                CustomerId = quotation.CustomerId,
                WarehouseId = warehouseId,
                CashSessionId = dto.CashSessionId,
                DiscountAmount = 0,             // already baked into the line prices
                DiscountPercent = 0,
                Notes = $"محوّل من عرض سعر {quotation.QuotationNumber}",
                Items = quotation.Items.Select(i => new CreateSaleItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    DiscountAmount = i.DiscountAmount,
                    DiscountPercent = i.DiscountPercent,
                }).ToList(),
                Payments = dto.Payments,
            };

            var sale = await _saleService.CreateAsync(createDto, cashierUserId);

            quotation.Status = QuotationStatus.Converted;
            quotation.ConvertedSaleId = sale.Id;
            await _context.SaveChangesAsync(ct);
            return sale.Id;
        }

        private async Task<string> NextNumberAsync(CancellationToken ct)
        {
            var prefix = $"QT-{DateTime.UtcNow:yyyyMM}-";
            var last = await _context.Quotations
                .Where(q => q.QuotationNumber.StartsWith(prefix))
                .OrderByDescending(q => q.QuotationNumber)
                .Select(q => q.QuotationNumber)
                .FirstOrDefaultAsync(ct);
            var next = 1;
            if (last != null && int.TryParse(last[prefix.Length..], out var n)) next = n + 1;
            return $"{prefix}{next:D4}";
        }

        private static QuotationDto Map(Quotation q, bool includeItems = true) => new()
        {
            Id = q.Id,
            QuotationNumber = q.QuotationNumber,
            CustomerId = q.CustomerId,
            CustomerName = q.Customer?.Name,
            CustomerNameSnapshot = q.CustomerNameSnapshot,
            CustomerPhoneSnapshot = q.CustomerPhoneSnapshot,
            WarehouseId = q.WarehouseId,
            IssueDate = q.IssueDate,
            ValidUntil = q.ValidUntil,
            SubTotal = q.SubTotal,
            DiscountAmount = q.DiscountAmount,
            DiscountPercent = q.DiscountPercent,
            VatAmount = q.VatAmount,
            Total = q.Total,
            Status = q.Status,
            ConvertedSaleId = q.ConvertedSaleId,
            Notes = q.Notes,
            Terms = q.Terms,
            CreatedAt = q.CreatedAt,
            Items = includeItems
                ? (q.Items ?? Enumerable.Empty<QuotationItem>()).Select(i => new QuotationItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductNameSnapshot = i.ProductNameSnapshot,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    DiscountAmount = i.DiscountAmount,
                    DiscountPercent = i.DiscountPercent,
                    VatRate = i.VatRate,
                    VatAmount = i.VatAmount,
                    LineSubTotal = i.LineSubTotal,
                    LineTotal = i.LineTotal,
                }).ToList()
                : new List<QuotationItemDto>(),
        };
    }
}
