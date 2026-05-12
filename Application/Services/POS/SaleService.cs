using Application.DTOs.POS;
using Application.Inerfaces.Inventory;
using Application.Inerfaces.POS;
using Domain.Enums;
using Domain.Models.POS;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.POS
{
    public class SaleService : ISaleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockService _stockService;

        public SaleService(ApplicationDbContext context, IStockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        public async Task<List<SaleDto>> GetAllAsync(SaleFilterDto filter)
        {
            var query = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Warehouse)
                .Include(s => s.Items)!.ThenInclude(i => i.Product)
                .Include(s => s.Payments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(s => s.InvoiceNumber.Contains(filter.Search.Trim()));

            if (filter.CustomerId.HasValue)
                query = query.Where(s => s.CustomerId == filter.CustomerId.Value);

            if (filter.CashierUserId.HasValue)
                query = query.Where(s => s.CashierUserId == filter.CashierUserId.Value);

            if (filter.WarehouseId.HasValue)
                query = query.Where(s => s.WarehouseId == filter.WarehouseId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(s => s.SaleDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(s => s.SaleDate <= filter.ToDate.Value);

            if (filter.Status.HasValue)
                query = query.Where(s => s.Status == filter.Status.Value);

            query = query.OrderByDescending(s => s.SaleDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize);

            var list = await query.ToListAsync();
            return list.Select(Map).ToList();
        }

        public async Task<SaleDto?> GetByIdAsync(Guid id)
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Warehouse)
                .Include(s => s.Items)!.ThenInclude(i => i.Product)
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == id);

            return sale == null ? null : Map(sale);
        }

        public async Task<SaleDto> CreateAsync(CreateSaleDto dto, Guid cashierUserId)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("لا يمكن إنشاء فاتورة بدون أصناف");
            if (dto.Payments == null || dto.Payments.Count == 0)
                throw new InvalidOperationException("يجب إدخال طريقة دفع واحدة على الأقل");

            // Validate session is open
            var session = await _context.CashSessions.FindAsync(dto.CashSessionId)
                ?? throw new InvalidOperationException("جلسة الكاش غير موجودة");
            if (session.Status != CashSessionStatus.Open)
                throw new InvalidOperationException("جلسة الكاش مغلقة");

            // Load products
            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var sale = new Sale
            {
                InvoiceNumber = await GenerateSaleNumberAsync(),
                CustomerId = dto.CustomerId,
                WarehouseId = dto.WarehouseId,
                CashSessionId = dto.CashSessionId,
                CashierUserId = cashierUserId,
                Notes = dto.Notes,
                DiscountPercent = dto.DiscountPercent,
                Items = new List<SaleItem>(),
                Payments = new List<SalePayment>()
            };

            decimal subTotal = 0, totalVat = 0;

            foreach (var itemDto in dto.Items)
            {
                if (!products.TryGetValue(itemDto.ProductId, out var product))
                    throw new InvalidOperationException($"المنتج {itemDto.ProductId} غير موجود");

                var unitPrice = itemDto.UnitPrice ?? product.SalePrice;
                if (unitPrice < product.MinSalePrice)
                    throw new InvalidOperationException(
                        $"سعر البيع لـ {product.NameAr} أقل من الحد الأدنى");

                var lineSubBefore = itemDto.Quantity * unitPrice;
                var lineDiscount = itemDto.DiscountAmount
                    + (lineSubBefore * itemDto.DiscountPercent / 100m);
                var lineSub = lineSubBefore - lineDiscount;
                var vatAmount = lineSub * (product.VatRate / 100m);
                var lineTotal = lineSub + vatAmount;

                sale.Items.Add(new SaleItem
                {
                    ProductId = product.Id,
                    ProductNameSnapshot = product.NameAr,
                    Quantity = itemDto.Quantity,
                    UnitPrice = unitPrice,
                    UnitCost = product.PurchasePrice,
                    DiscountAmount = lineDiscount,
                    DiscountPercent = itemDto.DiscountPercent,
                    VatRate = product.VatRate,
                    VatAmount = vatAmount,
                    LineSubTotal = lineSub,
                    LineTotal = lineTotal
                });

                subTotal += lineSubBefore;
                totalVat += vatAmount;
            }

            // Apply invoice-level discount (after lines)
            decimal invoiceDiscount = dto.DiscountAmount
                + (subTotal * dto.DiscountPercent / 100m);

            sale.SubTotal = subTotal;
            sale.DiscountAmount = invoiceDiscount + sale.Items.Sum(i => i.DiscountAmount);
            sale.VatAmount = totalVat;
            sale.Total = subTotal - invoiceDiscount - sale.Items.Sum(i => i.DiscountAmount) + totalVat;

            // Payments
            foreach (var p in dto.Payments)
            {
                sale.Payments.Add(new SalePayment
                {
                    Method = p.Method,
                    Amount = p.Amount,
                    Reference = p.Reference
                });
            }

            sale.PaidAmount = sale.Payments.Sum(p => p.Amount);
            sale.ChangeAmount = Math.Max(0, sale.PaidAmount - sale.Total);

            if (sale.PaidAmount < sale.Total
                && !sale.Payments.Any(p => p.Method == PaymentMethod.Credit))
                throw new InvalidOperationException("المبلغ المدفوع أقل من إجمالي الفاتورة");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                foreach (var item in sale.Items)
                {
                    await _stockService.ApplyMovementAsync(item.ProductId, sale.WarehouseId,
                        MovementType.SaleOut, item.Quantity, item.UnitCost,
                        sale.Id, "Sale", sale.InvoiceNumber, cashierUserId);
                }

                if (sale.CustomerId.HasValue && sale.PaidAmount < sale.Total)
                {
                    var customer = await _context.Customers.FindAsync(sale.CustomerId.Value);
                    if (customer != null)
                    {
                        customer.Balance += (sale.Total - sale.PaidAmount);
                        await _context.SaveChangesAsync();
                    }
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return (await GetByIdAsync(sale.Id))!;
        }

        public async Task<bool> CancelAsync(Guid id)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale == null || sale.Status == SaleStatus.Cancelled) return false;

            sale.Status = SaleStatus.Cancelled;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SaleDto?> RefundAsync(Guid id, string? reason, Guid? userId)
        {
            var sale = await _context.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sale == null || sale.Status != SaleStatus.Completed) return null;

            var returnNumber = $"RT-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
            var saleReturn = new SaleReturn
            {
                ReturnNumber = returnNumber,
                OriginalSaleId = sale.Id,
                CustomerId = sale.CustomerId,
                CashSessionId = sale.CashSessionId,
                Reason = reason,
                ProcessedByUserId = userId,
                SubTotal = sale.SubTotal - sale.DiscountAmount,
                VatAmount = sale.VatAmount,
                Total = sale.Total,
                Items = sale.Items?.Select(i => new SaleReturnItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    VatAmount = i.VatAmount,
                    LineTotal = i.LineTotal
                }).ToList()
            };

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.SaleReturns.Add(saleReturn);
                sale.Status = SaleStatus.Refunded;
                await _context.SaveChangesAsync();

                foreach (var item in sale.Items ?? new List<SaleItem>())
                {
                    await _stockService.ApplyMovementAsync(item.ProductId, sale.WarehouseId,
                        MovementType.ReturnIn, item.Quantity, item.UnitCost,
                        saleReturn.Id, "SaleReturn", returnNumber, userId);
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return await GetByIdAsync(id);
        }

        private async Task<string> GenerateSaleNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month;
            var count = await _context.Sales
                .CountAsync(s => s.SaleDate.Year == year && s.SaleDate.Month == month);
            return $"INV-{year:D4}{month:D2}-{(count + 1):D6}";
        }

        private static SaleDto Map(Sale s) => new()
        {
            Id = s.Id,
            InvoiceNumber = s.InvoiceNumber,
            CustomerId = s.CustomerId,
            CustomerName = s.Customer?.Name,
            WarehouseId = s.WarehouseId,
            WarehouseName = s.Warehouse?.NameAr,
            CashSessionId = s.CashSessionId,
            CashierUserId = s.CashierUserId,
            SaleDate = s.SaleDate,
            SubTotal = s.SubTotal,
            DiscountAmount = s.DiscountAmount,
            DiscountPercent = s.DiscountPercent,
            VatAmount = s.VatAmount,
            Total = s.Total,
            PaidAmount = s.PaidAmount,
            ChangeAmount = s.ChangeAmount,
            Status = s.Status,
            EInvoiceUuid = s.EInvoiceUuid,
            EInvoiceStatus = s.EInvoiceStatus,
            Notes = s.Notes,
            Items = s.Items?.Select(i => new SaleItemDto
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
                LineTotal = i.LineTotal
            }).ToList() ?? new(),
            Payments = s.Payments?.Select(p => new SalePaymentDto
            {
                Id = p.Id,
                Method = p.Method,
                Amount = p.Amount,
                Reference = p.Reference,
                PaidAt = p.PaidAt
            }).ToList() ?? new()
        };
    }
}
