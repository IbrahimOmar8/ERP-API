using Application.Services.Reports;
using Domain.Enums;
using Domain.Models.Auth;
using Domain.Models.Egypt;
using Domain.Models.Inventory;
using Domain.Models.POS;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tests.Reports;

public class ReportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _ctx;

    public ReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _ctx = new ApplicationDbContext(options);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task GetDashboardAsync_AggregatesTodaySalesCorrectly()
    {
        var warehouseId = await SeedWarehouse();
        var sessionId = await SeedSession(warehouseId);
        var product = await SeedProduct();

        var saleToday = NewSale(warehouseId, sessionId, DateTime.UtcNow);
        saleToday.SubTotal = 100m;
        saleToday.VatAmount = 14m;
        saleToday.Total = 114m;
        saleToday.Items = new List<SaleItem>
        {
            new()
            {
                ProductId = product.Id,
                ProductNameSnapshot = product.NameAr,
                Quantity = 1, UnitPrice = 100m, UnitCost = 60m, LineTotal = 114m
            }
        };
        _ctx.Sales.Add(saleToday);
        await _ctx.SaveChangesAsync();

        var report = await new ReportService(_ctx).GetDashboardAsync();
        report.TodaySales.Should().Be(114m);
        report.TodayInvoiceCount.Should().Be(1);
        report.TodayProfit.Should().Be(40m); // (100 - 60) * 1 - 0
    }

    [Fact]
    public async Task GetSalesReportAsync_GroupsByDate()
    {
        var warehouseId = await SeedWarehouse();
        var sessionId = await SeedSession(warehouseId);
        var product = await SeedProduct();
        var day = DateTime.UtcNow.Date.AddDays(-1);

        _ctx.Sales.Add(NewSale(warehouseId, sessionId, day, total: 50m, vat: 5m, sub: 45m));
        _ctx.Sales.Add(NewSale(warehouseId, sessionId, day.AddHours(2), total: 30m, vat: 3m, sub: 27m));
        await _ctx.SaveChangesAsync();

        var from = day.AddHours(-1);
        var to = day.AddDays(1);
        var report = await new ReportService(_ctx).GetSalesReportAsync(from, to, null);

        report.Rows.Should().HaveCount(1);
        report.Rows[0].InvoiceCount.Should().Be(2);
        report.TotalGross.Should().Be(80m);
        report.TotalVat.Should().Be(8m);
    }

    [Fact]
    public async Task GetStockReportAsync_FlagsLowStock()
    {
        var product = await SeedProduct(minStock: 10);
        var warehouseId = await SeedWarehouse();
        _ctx.StockItems.Add(new StockItem
        {
            ProductId = product.Id,
            WarehouseId = warehouseId,
            Quantity = 5,
            AverageCost = 60m
        });
        await _ctx.SaveChangesAsync();

        var rows = await new ReportService(_ctx).GetStockReportAsync(null, false);
        rows.Should().HaveCount(1);
        rows[0].IsLow.Should().BeTrue();
        rows[0].StockValue.Should().Be(300m); // 5 * 60
    }

    // ─── helpers ─────────────────────────────────────────────────────────

    private async Task<Guid> SeedWarehouse()
    {
        var w = new Warehouse { NameAr = "الرئيسي", Code = "MAIN", IsMain = true };
        _ctx.Warehouses.Add(w);
        await _ctx.SaveChangesAsync();
        return w.Id;
    }

    private async Task<Guid> SeedSession(Guid warehouseId)
    {
        var register = new CashRegister
            { Name = "Reg1", Code = "R1", WarehouseId = warehouseId };
        _ctx.CashRegisters.Add(register);
        var session = new CashSession
        {
            CashRegisterId = register.Id,
            CashierUserId = Guid.NewGuid(),
            OpeningBalance = 0,
            Status = CashSessionStatus.Open
        };
        _ctx.CashSessions.Add(session);
        await _ctx.SaveChangesAsync();
        return session.Id;
    }

    private async Task<Product> SeedProduct(decimal minStock = 0)
    {
        var category = new Category { NameAr = "عام" };
        var unit = new Unit { NameAr = "قطعة", Code = "PC" };
        _ctx.Categories.Add(category);
        _ctx.Units.Add(unit);
        await _ctx.SaveChangesAsync();

        var product = new Product
        {
            Sku = "P-1",
            NameAr = "صنف",
            CategoryId = category.Id,
            UnitId = unit.Id,
            PurchasePrice = 60m,
            SalePrice = 100m,
            VatRate = 14m,
            MinStockLevel = minStock,
        };
        _ctx.Products.Add(product);
        await _ctx.SaveChangesAsync();
        return product;
    }

    private static Sale NewSale(
        Guid warehouseId,
        Guid sessionId,
        DateTime date,
        decimal total = 100m,
        decimal vat = 14m,
        decimal sub = 100m)
        => new()
        {
            InvoiceNumber = "I-" + Guid.NewGuid().ToString("N").Substring(0, 6),
            WarehouseId = warehouseId,
            CashSessionId = sessionId,
            CashierUserId = Guid.NewGuid(),
            SaleDate = date,
            SubTotal = sub,
            VatAmount = vat,
            Total = total,
            Status = SaleStatus.Completed,
        };
}
