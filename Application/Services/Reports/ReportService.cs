using Application.DTOs.Reports;
using Application.Inerfaces.Reports;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Reports
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context) => _context = context;

        public async Task<DashboardKpiDto> GetDashboardAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var sales = _context.Sales.Where(s => s.Status == SaleStatus.Completed);

            var today = await sales
                .Where(s => s.SaleDate >= todayStart)
                .Select(s => new { s.Total, s.SubTotal })
                .ToListAsync(ct);
            var month = await sales
                .Where(s => s.SaleDate >= monthStart)
                .Select(s => new { s.Total, s.SubTotal })
                .ToListAsync(ct);

            var todayProfit = await ComputeProfitAsync(todayStart, now, ct);
            var monthProfit = await ComputeProfitAsync(monthStart, now, ct);

            var todayExpenses = await _context.Expenses
                .Where(e => e.ExpenseDate >= todayStart && e.ExpenseDate < now)
                .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;
            var monthExpenses = await _context.Expenses
                .Where(e => e.ExpenseDate >= monthStart && e.ExpenseDate < now)
                .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

            return new DashboardKpiDto
            {
                TodaySales = today.Sum(x => x.Total),
                TodayInvoiceCount = today.Count,
                TodayProfit = todayProfit,
                TodayExpenses = todayExpenses,
                TodayNetProfit = todayProfit - todayExpenses,
                MonthSales = month.Sum(x => x.Total),
                MonthInvoiceCount = month.Count,
                MonthProfit = monthProfit,
                MonthExpenses = monthExpenses,
                MonthNetProfit = monthProfit - monthExpenses,
                CustomerCount = await _context.Customers.CountAsync(c => c.IsActive, ct),
                ProductCount = await _context.Products.CountAsync(p => p.IsActive, ct),
                LowStockCount = await _context.StockItems
                    .Include(s => s.Product)
                    .Where(s => s.Product != null && s.Quantity <= s.Product.MinStockLevel)
                    .CountAsync(ct),
                OpenSessionCount = await _context.CashSessions
                    .CountAsync(s => s.Status == CashSessionStatus.Open, ct),
                TotalStockValue = await _context.StockItems.SumAsync(s => s.Quantity * s.AverageCost, ct)
            };
        }

        public async Task<IReadOnlyList<TopCustomerRow>> GetTopCustomersAsync(DateTime from, DateTime to, int take, CancellationToken ct = default)
        {
            return await _context.Sales
                .Where(s => s.Status == SaleStatus.Completed
                            && s.SaleDate >= from && s.SaleDate < to
                            && s.CustomerId != null
                            && s.Customer != null)
                .GroupBy(s => new { s.CustomerId, s.Customer!.Name })
                .Select(g => new TopCustomerRow
                {
                    CustomerId = g.Key.CustomerId!.Value,
                    CustomerName = g.Key.Name,
                    InvoiceCount = g.Count(),
                    TotalSpent = g.Sum(s => s.Total),
                    LastPurchase = g.Max(s => s.SaleDate),
                })
                .OrderByDescending(r => r.TotalSpent)
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync(ct);
        }

        public async Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to, Guid? warehouseId, CancellationToken ct = default)
        {
            var sales = _context.Sales
                .Where(s => s.Status == SaleStatus.Completed && s.SaleDate >= from && s.SaleDate < to);
            if (warehouseId.HasValue)
                sales = sales.Where(s => s.WarehouseId == warehouseId.Value);

            var rows = await sales
                .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month, s.SaleDate.Day })
                .Select(g => new
                {
                    g.Key.Year, g.Key.Month, g.Key.Day,
                    Count = g.Count(),
                    Net = g.Sum(x => x.SubTotal - x.DiscountAmount),
                    Vat = g.Sum(x => x.VatAmount),
                    Total = g.Sum(x => x.Total)
                })
                .ToListAsync(ct);

            var items = await _context.SaleItems
                .Where(i => i.Sale != null && i.Sale.Status == SaleStatus.Completed
                            && i.Sale.SaleDate >= from && i.Sale.SaleDate < to
                            && (!warehouseId.HasValue || i.Sale.WarehouseId == warehouseId.Value))
                .Select(i => new
                {
                    i.Sale!.SaleDate.Year, i.Sale.SaleDate.Month, i.Sale.SaleDate.Day,
                    Profit = (i.UnitPrice - i.UnitCost) * i.Quantity - i.DiscountAmount
                })
                .ToListAsync(ct);

            var profitByDay = items
                .GroupBy(x => new { x.Year, x.Month, x.Day })
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Profit));

            var result = new SalesReportDto { From = from, To = to };
            foreach (var r in rows.OrderBy(r => r.Year).ThenBy(r => r.Month).ThenBy(r => r.Day))
            {
                profitByDay.TryGetValue(new { r.Year, r.Month, r.Day }, out var profit);
                result.Rows.Add(new SalesReportRow
                {
                    Date = new DateTime(r.Year, r.Month, r.Day, 0, 0, 0, DateTimeKind.Utc),
                    InvoiceCount = r.Count,
                    NetSales = r.Net,
                    VatAmount = r.Vat,
                    TotalSales = r.Total,
                    Profit = profit
                });
            }

            result.TotalNetSales = result.Rows.Sum(r => r.NetSales);
            result.TotalVat = result.Rows.Sum(r => r.VatAmount);
            result.TotalGross = result.Rows.Sum(r => r.TotalSales);
            result.TotalProfit = result.Rows.Sum(r => r.Profit);
            result.TotalInvoices = result.Rows.Sum(r => r.InvoiceCount);
            return result;
        }

        public async Task<IReadOnlyList<TopProductRow>> GetTopProductsAsync(DateTime from, DateTime to, int take, CancellationToken ct = default)
        {
            return await _context.SaleItems
                .Where(i => i.Sale != null
                            && i.Sale.Status == SaleStatus.Completed
                            && i.Sale.SaleDate >= from
                            && i.Sale.SaleDate < to)
                .GroupBy(i => new { i.ProductId, i.ProductNameSnapshot })
                .Select(g => new TopProductRow
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductNameSnapshot,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.LineTotal),
                    Profit = g.Sum(x => (x.UnitPrice - x.UnitCost) * x.Quantity - x.DiscountAmount)
                })
                .OrderByDescending(r => r.Revenue)
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<StockReportRow>> GetStockReportAsync(Guid? warehouseId, bool onlyLow, CancellationToken ct = default)
        {
            var q = _context.StockItems
                .Include(s => s.Product)
                .Include(s => s.Warehouse)
                .AsQueryable();
            if (warehouseId.HasValue)
                q = q.Where(s => s.WarehouseId == warehouseId.Value);

            var rows = await q.Select(s => new StockReportRow
            {
                ProductId = s.ProductId,
                ProductName = s.Product != null ? s.Product.NameAr : string.Empty,
                Sku = s.Product != null ? s.Product.Sku : string.Empty,
                WarehouseId = s.WarehouseId,
                WarehouseName = s.Warehouse != null ? s.Warehouse.NameAr : string.Empty,
                Quantity = s.Quantity,
                AverageCost = s.AverageCost,
                StockValue = s.Quantity * s.AverageCost,
                MinQuantity = s.Product != null ? s.Product.MinStockLevel : 0,
                IsLow = s.Product != null && s.Quantity <= s.Product.MinStockLevel
            }).ToListAsync(ct);

            return onlyLow ? rows.Where(r => r.IsLow).ToList() : rows;
        }

        public async Task<CashSessionReportDto?> GetCashSessionReportAsync(Guid sessionId, CancellationToken ct = default)
        {
            var session = await _context.CashSessions
                .Include(s => s.CashRegister)
                .Include(s => s.Sales!).ThenInclude(x => x.Payments)
                .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
            if (session == null) return null;

            var cashier = await _context.Users
                .Where(u => u.Id == session.CashierUserId)
                .Select(u => u.FullName ?? u.UserName)
                .FirstOrDefaultAsync(ct) ?? string.Empty;

            var sales = session.Sales?.Where(x => x.Status == SaleStatus.Completed).ToList() ?? new();
            var payments = sales.SelectMany(s => s.Payments ?? Enumerable.Empty<Domain.Models.POS.SalePayment>()).ToList();

            var cash = payments.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.Amount);
            var card = payments.Where(p => p.Method == PaymentMethod.Card).Sum(p => p.Amount);
            var other = payments.Where(p => p.Method != PaymentMethod.Cash && p.Method != PaymentMethod.Card).Sum(p => p.Amount);

            var expectedCash = session.OpeningBalance + cash;
            var actualCash = session.Status == CashSessionStatus.Open
                ? expectedCash
                : session.ClosingBalance;

            return new CashSessionReportDto
            {
                SessionId = session.Id,
                OpenedAt = session.OpenedAt,
                ClosedAt = session.ClosedAt,
                CashierName = cashier,
                RegisterName = session.CashRegister?.Name ?? string.Empty,
                OpeningBalance = session.OpeningBalance,
                ExpectedCash = expectedCash,
                ActualCash = actualCash,
                CashDelta = actualCash - expectedCash,
                InvoiceCount = sales.Count,
                NetSales = sales.Sum(s => s.SubTotal - s.DiscountAmount),
                VatAmount = sales.Sum(s => s.VatAmount),
                TotalSales = sales.Sum(s => s.Total),
                CashPayments = cash,
                CardPayments = card,
                OtherPayments = other
            };
        }

        public async Task<ProfitLossReportDto> GetProfitLossAsync(DateTime from, DateTime to, CancellationToken ct = default)
        {
            var completedSales = _context.Sales
                .Where(s => s.Status == SaleStatus.Completed && s.SaleDate >= from && s.SaleDate < to);
            var refundedSales = _context.Sales
                .Where(s => (s.Status == SaleStatus.Refunded || s.Status == SaleStatus.PartiallyRefunded)
                            && s.SaleDate >= from && s.SaleDate < to);

            var grossSales = await completedSales.SumAsync(s => (decimal?)s.SubTotal, ct) ?? 0m;
            var discounts = await completedSales.SumAsync(s => (decimal?)s.DiscountAmount, ct) ?? 0m;
            var refunds = await refundedSales.SumAsync(s => (decimal?)s.Total, ct) ?? 0m;

            var cogs = await _context.SaleItems
                .Where(i => i.Sale != null
                            && i.Sale.Status == SaleStatus.Completed
                            && i.Sale.SaleDate >= from
                            && i.Sale.SaleDate < to)
                .SumAsync(i => (decimal?)(i.UnitCost * i.Quantity), ct) ?? 0m;

            var expensesQuery = _context.Expenses
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate < to);
            var operatingExpenses = await expensesQuery.SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

            var expensesByCategory = await expensesQuery
                .GroupBy(e => e.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Amount = g.Sum(e => e.Amount),
                })
                .ToListAsync(ct);

            var report = new ProfitLossReportDto
            {
                From = from,
                To = to,
                GrossSales = grossSales,
                Discounts = discounts,
                Refunds = refunds,
                CostOfGoodsSold = cogs,
                OperatingExpenses = operatingExpenses,
                ExpensesByCategory = expensesByCategory
                    .OrderByDescending(x => x.Amount)
                    .Select(x => new ExpenseLine
                    {
                        Category = x.Category.ToString(),
                        CategoryId = (int)x.Category,
                        Amount = x.Amount,
                        PercentOfTotal = operatingExpenses == 0
                            ? 0
                            : Math.Round(x.Amount / operatingExpenses * 100, 2),
                    })
                    .ToList(),
            };
            return report;
        }

        public async Task<CashFlowReportDto> GetCashFlowAsync(DateTime from, DateTime to, CancellationToken ct = default)
        {
            // Inflows: payments on completed sales, by method
            var paymentsQuery = _context.SalePayments
                .Where(p => p.Sale != null
                            && p.Sale.Status == SaleStatus.Completed
                            && p.PaidAt >= from && p.PaidAt < to);

            var paymentsByMethod = await paymentsQuery
                .GroupBy(p => p.Method)
                .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Amount) })
                .ToListAsync(ct);

            decimal cashIn = paymentsByMethod.Where(x => x.Method == PaymentMethod.Cash).Sum(x => x.Amount);
            decimal cardIn = paymentsByMethod.Where(x => x.Method == PaymentMethod.Card).Sum(x => x.Amount);
            decimal otherIn = paymentsByMethod
                .Where(x => x.Method != PaymentMethod.Cash && x.Method != PaymentMethod.Card)
                .Sum(x => x.Amount);

            // Outflows
            var purchasesOut = await _context.PurchaseInvoices
                .Where(p => p.InvoiceDate >= from && p.InvoiceDate < to)
                .SumAsync(p => (decimal?)p.Paid, ct) ?? 0m;

            var expensesOut = await _context.Expenses
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate < to)
                .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

            var refundsOut = await _context.SaleReturns
                .Where(r => r.ReturnDate >= from && r.ReturnDate < to)
                .SumAsync(r => (decimal?)r.Total, ct) ?? 0m;

            // Daily breakdown
            var paymentDays = await paymentsQuery
                .GroupBy(p => new { p.PaidAt.Year, p.PaidAt.Month, p.PaidAt.Day })
                .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, In = g.Sum(p => p.Amount) })
                .ToListAsync(ct);

            var purchaseDays = await _context.PurchaseInvoices
                .Where(p => p.InvoiceDate >= from && p.InvoiceDate < to)
                .GroupBy(p => new { p.InvoiceDate.Year, p.InvoiceDate.Month, p.InvoiceDate.Day })
                .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, Out_ = g.Sum(p => p.Paid) })
                .ToListAsync(ct);

            var expenseDays = await _context.Expenses
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate < to)
                .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month, e.ExpenseDate.Day })
                .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, Out_ = g.Sum(e => e.Amount) })
                .ToListAsync(ct);

            var daily = new Dictionary<DateTime, (decimal In, decimal Out)>();
            foreach (var d in paymentDays)
            {
                var dt = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
                daily[dt] = (d.In, 0);
            }
            foreach (var d in purchaseDays)
            {
                var dt = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
                var cur = daily.GetValueOrDefault(dt);
                daily[dt] = (cur.In, cur.Out + d.Out_);
            }
            foreach (var d in expenseDays)
            {
                var dt = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
                var cur = daily.GetValueOrDefault(dt);
                daily[dt] = (cur.In, cur.Out + d.Out_);
            }

            return new CashFlowReportDto
            {
                From = from,
                To = to,
                CashSalesIn = cashIn,
                CardSalesIn = cardIn,
                OtherSalesIn = otherIn,
                PurchasesOut = purchasesOut,
                ExpensesOut = expensesOut,
                RefundsOut = refundsOut,
                Daily = daily
                    .OrderBy(kv => kv.Key)
                    .Select(kv => new CashFlowDailyRow
                    {
                        Date = kv.Key,
                        In = kv.Value.In,
                        Out = kv.Value.Out,
                    })
                    .ToList(),
            };
        }

        public async Task<IReadOnlyList<InventoryAgingRow>> GetInventoryAgingAsync(int? bucketDays, CancellationToken ct = default)
        {
            // Aggregate stock per product (across warehouses) then attach the
            // last sale date so we can bucket "days since last sale".
            var stock = await _context.StockItems
                .Include(s => s.Product)
                .Where(s => s.Product != null
                            && s.Product.IsActive
                            && s.Product.TrackStock
                            && s.Quantity > 0)
                .GroupBy(s => new { s.ProductId, s.Product!.NameAr, s.Product.Sku })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.NameAr,
                    g.Key.Sku,
                    Quantity = g.Sum(x => x.Quantity),
                    AverageCost = g.Average(x => x.AverageCost),
                })
                .ToListAsync(ct);

            var productIds = stock.Select(s => s.ProductId).ToList();
            var lastSold = await _context.SaleItems
                .Where(i => productIds.Contains(i.ProductId)
                            && i.Sale != null
                            && i.Sale.Status == SaleStatus.Completed)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, LastSoldAt = g.Max(x => x.Sale!.SaleDate) })
                .ToDictionaryAsync(x => x.ProductId, x => x.LastSoldAt, ct);

            var now = DateTime.UtcNow;
            return stock.Select(s =>
            {
                lastSold.TryGetValue(s.ProductId, out var lastDate);
                var days = lastDate == default ? -1 : (int)(now - lastDate).TotalDays;
                var bucket = days switch
                {
                    -1 => 4,                          // never sold
                    <= 30 => 0,
                    <= 60 => 1,
                    <= 90 => 2,
                    <= 180 => 3,
                    _ => 4,
                };
                return new InventoryAgingRow
                {
                    ProductId = s.ProductId,
                    ProductName = s.NameAr,
                    Sku = s.Sku,
                    Quantity = s.Quantity,
                    AverageCost = s.AverageCost,
                    StockValue = s.Quantity * s.AverageCost,
                    LastSoldAt = lastDate == default ? null : lastDate,
                    DaysSinceLastSale = days,
                    Bucket = bucket,
                };
            })
            .Where(r => !bucketDays.HasValue
                        || r.DaysSinceLastSale < 0
                        || r.DaysSinceLastSale >= bucketDays.Value)
            .OrderByDescending(r => r.DaysSinceLastSale)
            .ToList();
        }

        public async Task<IReadOnlyList<CashierPerformanceRow>> GetCashierPerformanceAsync(DateTime from, DateTime to, CancellationToken ct = default)
        {
            // Sales aggregates per cashier
            var sales = await _context.Sales
                .Where(s => s.Status == SaleStatus.Completed
                            && s.SaleDate >= from && s.SaleDate < to)
                .GroupBy(s => s.CashierUserId)
                .Select(g => new
                {
                    CashierUserId = g.Key,
                    InvoiceCount = g.Count(),
                    TotalSales = g.Sum(x => x.Total),
                })
                .ToListAsync(ct);

            // Refunds processed by each cashier
            var refunds = await _context.SaleReturns
                .Where(r => r.ProcessedByUserId != null
                            && r.ReturnDate >= from && r.ReturnDate < to)
                .GroupBy(r => r.ProcessedByUserId!.Value)
                .Select(g => new
                {
                    CashierUserId = g.Key,
                    RefundCount = g.Count(),
                    RefundsAmount = g.Sum(x => x.Total),
                })
                .ToDictionaryAsync(x => x.CashierUserId, ct);

            // Cashier names
            var cashierIds = sales.Select(s => s.CashierUserId)
                .Union(refunds.Keys)
                .Distinct()
                .ToList();
            var names = await _context.Users
                .Where(u => cashierIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.UserName })
                .ToDictionaryAsync(u => u.Id, u => string.IsNullOrEmpty(u.FullName) ? u.UserName : u.FullName, ct);

            var rows = sales.Select(s =>
            {
                refunds.TryGetValue(s.CashierUserId, out var r);
                names.TryGetValue(s.CashierUserId, out var name);
                return new CashierPerformanceRow
                {
                    CashierUserId = s.CashierUserId,
                    CashierName = name ?? "—",
                    InvoiceCount = s.InvoiceCount,
                    TotalSales = s.TotalSales,
                    AverageTicket = s.InvoiceCount > 0 ? s.TotalSales / s.InvoiceCount : 0,
                    RefundCount = r?.RefundCount ?? 0,
                    RefundsAmount = r?.RefundsAmount ?? 0,
                };
            }).ToList();

            // Cashiers who only had refunds (no sales) in the period
            foreach (var (cashierId, r) in refunds)
            {
                if (rows.Any(x => x.CashierUserId == cashierId)) continue;
                names.TryGetValue(cashierId, out var name);
                rows.Add(new CashierPerformanceRow
                {
                    CashierUserId = cashierId,
                    CashierName = name ?? "—",
                    RefundCount = r.RefundCount,
                    RefundsAmount = r.RefundsAmount,
                });
            }

            return rows.OrderByDescending(r => r.TotalSales).ToList();
        }

        private async Task<decimal> ComputeProfitAsync(DateTime from, DateTime to, CancellationToken ct)
        {
            return await _context.SaleItems
                .Where(i => i.Sale != null && i.Sale.Status == SaleStatus.Completed
                            && i.Sale.SaleDate >= from && i.Sale.SaleDate < to)
                .SumAsync(i => (i.UnitPrice - i.UnitCost) * i.Quantity - i.DiscountAmount, ct);
        }
    }
}
