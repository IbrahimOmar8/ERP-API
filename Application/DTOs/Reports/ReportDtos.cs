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
