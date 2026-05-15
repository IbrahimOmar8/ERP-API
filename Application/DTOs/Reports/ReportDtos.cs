namespace Application.DTOs.Reports
{
    public class DashboardKpiDto
    {
        public decimal TodaySales { get; set; }
        public int TodayInvoiceCount { get; set; }
        public decimal TodayProfit { get; set; }
        public decimal TodayExpenses { get; set; }
        public decimal TodayNetProfit { get; set; }
        public decimal MonthSales { get; set; }
        public int MonthInvoiceCount { get; set; }
        public decimal MonthProfit { get; set; }
        public decimal MonthExpenses { get; set; }
        public decimal MonthNetProfit { get; set; }
        public int CustomerCount { get; set; }
        public int ProductCount { get; set; }
        public int LowStockCount { get; set; }
        public int OpenSessionCount { get; set; }
        public decimal TotalStockValue { get; set; }
    }

    public class TopCustomerRow
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastPurchase { get; set; }
    }

    public class ProfitLossReportDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // Revenue
        public decimal GrossSales { get; set; }
        public decimal Discounts { get; set; }
        public decimal Refunds { get; set; }
        public decimal NetSales => GrossSales - Discounts - Refunds;

        // Cost of goods sold
        public decimal CostOfGoodsSold { get; set; }
        public decimal GrossProfit => NetSales - CostOfGoodsSold;
        public decimal GrossMarginPercent => NetSales == 0 ? 0 : Math.Round(GrossProfit / NetSales * 100, 2);

        // Operating expenses
        public decimal OperatingExpenses { get; set; }
        public List<ExpenseLine> ExpensesByCategory { get; set; } = new();

        // Net
        public decimal NetProfit => GrossProfit - OperatingExpenses;
        public decimal NetMarginPercent => NetSales == 0 ? 0 : Math.Round(NetProfit / NetSales * 100, 2);
    }

    public class ExpenseLine
    {
        public string Category { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public decimal PercentOfTotal { get; set; }
    }

    public class CashFlowReportDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public decimal CashSalesIn { get; set; }
        public decimal CardSalesIn { get; set; }
        public decimal OtherSalesIn { get; set; }
        public decimal TotalIn => CashSalesIn + CardSalesIn + OtherSalesIn;

        public decimal PurchasesOut { get; set; }
        public decimal ExpensesOut { get; set; }
        public decimal RefundsOut { get; set; }
        public decimal TotalOut => PurchasesOut + ExpensesOut + RefundsOut;

        public decimal NetCashFlow => TotalIn - TotalOut;

        public List<CashFlowDailyRow> Daily { get; set; } = new();
    }

    public class CashFlowDailyRow
    {
        public DateTime Date { get; set; }
        public decimal In { get; set; }
        public decimal Out { get; set; }
        public decimal Net => In - Out;
    }

    public class InventoryAgingRow
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AverageCost { get; set; }
        public decimal StockValue { get; set; }
        public DateTime? LastSoldAt { get; set; }
        public int DaysSinceLastSale { get; set; } // -1 if never sold
        public int Bucket { get; set; } // 0-30, 30-60, 60-90, 90-180, 180+
    }

    public class CashierPerformanceRow
    {
        public Guid CashierUserId { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal AverageTicket { get; set; }
        public int RefundCount { get; set; }
        public decimal RefundsAmount { get; set; }
        public decimal NetSales => TotalSales - RefundsAmount;
    }

    public class SalesReportRow
    {
        public DateTime Date { get; set; }
        public int InvoiceCount { get; set; }
        public decimal NetSales { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal Profit { get; set; }
    }

    public class SalesReportDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public List<SalesReportRow> Rows { get; set; } = new();
        public decimal TotalNetSales { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalGross { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalInvoices { get; set; }
    }

    public class TopProductRow
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
    }

    public class StockReportRow
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AverageCost { get; set; }
        public decimal StockValue { get; set; }
        public decimal MinQuantity { get; set; }
        public bool IsLow { get; set; }
    }

    public class CashSessionReportDto
    {
        public Guid SessionId { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public string RegisterName { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ActualCash { get; set; }
        public decimal CashDelta { get; set; }
        public int InvoiceCount { get; set; }
        public decimal NetSales { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal CashPayments { get; set; }
        public decimal CardPayments { get; set; }
        public decimal OtherPayments { get; set; }
    }
}
