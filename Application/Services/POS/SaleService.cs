using Application.DTOs.POS;
using Application.Inerfaces.Inventory;
using Application.Inerfaces.POS;
using Domain.Enums;
using Domain.Models.Loyalty;
using Domain.Models.POS;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.POS
{
    public class SaleService : ISaleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockService _stockService;
        private readonly Application.Inerfaces.Integration.IWebhookService _webhooks;

        public SaleService(
            ApplicationDbContext context,
            IStockService stockService,
            Application.Inerfaces.Integration.IWebhookService webhooks)
        {
            _context = context;
            _stockService = stockService;
            _webhooks = webhooks;
        }

        public async Task<List<SaleDto>> GetAllAsync(SaleFilterDto filter)
        {
            var query = _context.Sales
                .AsNoTracking()
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
                SalesmanId = dto.SalesmanId,
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

            // Resolve coupon (if any). The coupon discounts against the
            // post-line-discount subtotal so MinSubtotal etc. behave intuitively.
            Coupon? coupon = null;
            decimal couponDiscount = 0;
            if (!string.IsNullOrWhiteSpace(dto.CouponCode))
            {
                var code = dto.CouponCode.Trim().ToUpperInvariant();
                coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == code);
                if (coupon == null) throw new InvalidOperationException("الكوبون غير موجود");
                if (!coupon.IsActive) throw new InvalidOperationException("الكوبون غير مفعّل");
                var now = DateTime.UtcNow;
                if (coupon.ValidFrom.HasValue && now < coupon.ValidFrom.Value)
                    throw new InvalidOperationException("الكوبون لم يبدأ بعد");
                if (coupon.ValidTo.HasValue && now > coupon.ValidTo.Value)
                    throw new InvalidOperationException("انتهت صلاحية الكوبون");
                if (coupon.MaxUses.HasValue && coupon.UsageCount >= coupon.MaxUses.Value)
                    throw new InvalidOperationException("استُهلك الكوبون بالكامل");
                var afterLineDiscounts = subTotal - sale.Items.Sum(i => i.DiscountAmount);
                if (afterLineDiscounts < coupon.MinSubtotal)
                    throw new InvalidOperationException(
                        $"الحد الأدنى لاستخدام الكوبون: {coupon.MinSubtotal:N2}");
                if (coupon.MaxUsesPerCustomer.HasValue && dto.CustomerId.HasValue)
                {
                    var prev = await _context.Sales
                        .CountAsync(s => s.CouponId == coupon.Id && s.CustomerId == dto.CustomerId.Value);
                    if (prev >= coupon.MaxUsesPerCustomer.Value)
                        throw new InvalidOperationException("تجاوزت حد استخدام الكوبون لهذا العميل");
                }
                couponDiscount = coupon.Type == DiscountType.Percentage
                    ? afterLineDiscounts * (coupon.Value / 100m)
                    : coupon.Value;
                if (coupon.MaxDiscountAmount.HasValue && couponDiscount > coupon.MaxDiscountAmount.Value)
                    couponDiscount = coupon.MaxDiscountAmount.Value;
                couponDiscount = Math.Min(couponDiscount, afterLineDiscounts);
            }

            // Loyalty: redeem points
            decimal pointsValueApplied = 0;
            int pointsRedeemed = 0;
            LoyaltySettings? loyaltySettings = null;
            Customer? customer = dto.CustomerId.HasValue
                ? await _context.Customers.FindAsync(dto.CustomerId.Value)
                : null;

            if (dto.PointsToRedeem > 0)
            {
                if (customer == null)
                    throw new InvalidOperationException("استبدال النقاط يتطلب عميلاً مسجلاً");
                loyaltySettings = await _context.LoyaltySettings.FirstOrDefaultAsync()
                                  ?? new LoyaltySettings();
                if (!loyaltySettings.Enabled)
                    throw new InvalidOperationException("برنامج الولاء غير مفعّل");
                if (dto.PointsToRedeem > customer.LoyaltyPoints)
                    throw new InvalidOperationException("رصيد النقاط غير كافٍ");
                if (dto.PointsToRedeem < loyaltySettings.MinRedeemPoints)
                    throw new InvalidOperationException(
                        $"الحد الأدنى للاستبدال {loyaltySettings.MinRedeemPoints} نقطة");

                pointsRedeemed = dto.PointsToRedeem;
                pointsValueApplied = pointsRedeemed * loyaltySettings.PointValueEgp;
                var afterAll = subTotal - sale.Items.Sum(i => i.DiscountAmount)
                               - invoiceDiscount - couponDiscount;
                var maxRedeemValue = afterAll * (loyaltySettings.MaxRedeemPercent / 100m);
                if (pointsValueApplied > maxRedeemValue)
                {
                    pointsValueApplied = maxRedeemValue;
                    pointsRedeemed = (int)Math.Floor(pointsValueApplied / loyaltySettings.PointValueEgp);
                    pointsValueApplied = pointsRedeemed * loyaltySettings.PointValueEgp;
                }
            }

            sale.SubTotal = subTotal;
            sale.DiscountAmount = invoiceDiscount + sale.Items.Sum(i => i.DiscountAmount) + couponDiscount;
            sale.VatAmount = totalVat;
            sale.Total = Math.Max(0,
                subTotal
                - sale.Items.Sum(i => i.DiscountAmount)
                - invoiceDiscount
                - couponDiscount
                + totalVat
                - pointsValueApplied);

            sale.CouponId = coupon?.Id;
            sale.CouponCode = coupon?.Code;
            sale.CouponDiscount = couponDiscount;
            sale.PointsRedeemed = pointsRedeemed;
            sale.PointsValueApplied = pointsValueApplied;

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
                    var cu = await _context.Customers.FindAsync(sale.CustomerId.Value);
                    if (cu != null)
                    {
                        cu.Balance += (sale.Total - sale.PaidAmount);
                        await _context.SaveChangesAsync();
                    }
                }

                // Mark coupon as used (count is best-effort, not transactional)
                if (coupon != null)
                {
                    coupon.UsageCount += 1;
                    await _context.SaveChangesAsync();
                }

                // Loyalty: deduct redeemed points + earn new points
                if (customer != null)
                {
                    if (pointsRedeemed > 0)
                    {
                        customer.LoyaltyPoints -= pointsRedeemed;
                        _context.LoyaltyTransactions.Add(new LoyaltyTransaction
                        {
                            CustomerId = customer.Id,
                            Type = LoyaltyTxType.Redeem,
                            Points = -pointsRedeemed,
                            BalanceAfter = customer.LoyaltyPoints,
                            SaleId = sale.Id,
                            Notes = $"خصم على فاتورة {sale.InvoiceNumber}",
                            CreatedByUserId = cashierUserId,
                        });
                    }

                    loyaltySettings ??= await _context.LoyaltySettings.FirstOrDefaultAsync();
                    if (loyaltySettings is { Enabled: true } && loyaltySettings.EgpPerPointEarned > 0)
                    {
                        // Earn points on what the customer actually paid
                        var spent = sale.Total;
                        var earned = (int)Math.Floor(spent / loyaltySettings.EgpPerPointEarned);
                        if (earned > 0)
                        {
                            customer.LoyaltyPoints += earned;
                            sale.PointsEarned = earned;
                            _context.LoyaltyTransactions.Add(new LoyaltyTransaction
                            {
                                CustomerId = customer.Id,
                                Type = LoyaltyTxType.Earn,
                                Points = earned,
                                BalanceAfter = customer.LoyaltyPoints,
                                SaleId = sale.Id,
                                Notes = $"اكتساب من فاتورة {sale.InvoiceNumber}",
                                CreatedByUserId = cashierUserId,
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            var saved = (await GetByIdAsync(sale.Id))!;
            await _webhooks.DispatchAsync(Application.DTOs.Integration.WebhookEvents.SaleCreated, new
            {
                id = saved.Id,
                invoiceNumber = saved.InvoiceNumber,
                customerId = saved.CustomerId,
                total = saved.Total,
                saleDate = saved.SaleDate,
            });
            return saved;
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

            await _webhooks.DispatchAsync(Application.DTOs.Integration.WebhookEvents.SaleRefunded, new
            {
                saleId = sale.Id,
                invoiceNumber = sale.InvoiceNumber,
                total = sale.Total,
                reason,
            });
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
            SalesmanId = s.SalesmanId,
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
            CouponCode = s.CouponCode,
            CouponDiscount = s.CouponDiscount,
            PointsEarned = s.PointsEarned,
            PointsRedeemed = s.PointsRedeemed,
            PointsValueApplied = s.PointsValueApplied,
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
