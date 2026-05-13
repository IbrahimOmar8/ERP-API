class DashboardKpi {
  final double todaySales;
  final int todayInvoiceCount;
  final double todayProfit;
  final double monthSales;
  final int monthInvoiceCount;
  final double monthProfit;
  final int customerCount;
  final int productCount;
  final int lowStockCount;
  final int openSessionCount;
  final double totalStockValue;

  DashboardKpi({
    required this.todaySales,
    required this.todayInvoiceCount,
    required this.todayProfit,
    required this.monthSales,
    required this.monthInvoiceCount,
    required this.monthProfit,
    required this.customerCount,
    required this.productCount,
    required this.lowStockCount,
    required this.openSessionCount,
    required this.totalStockValue,
  });

  factory DashboardKpi.fromJson(Map<String, dynamic> j) => DashboardKpi(
        todaySales: (j['todaySales'] ?? 0).toDouble(),
        todayInvoiceCount: j['todayInvoiceCount'] ?? 0,
        todayProfit: (j['todayProfit'] ?? 0).toDouble(),
        monthSales: (j['monthSales'] ?? 0).toDouble(),
        monthInvoiceCount: j['monthInvoiceCount'] ?? 0,
        monthProfit: (j['monthProfit'] ?? 0).toDouble(),
        customerCount: j['customerCount'] ?? 0,
        productCount: j['productCount'] ?? 0,
        lowStockCount: j['lowStockCount'] ?? 0,
        openSessionCount: j['openSessionCount'] ?? 0,
        totalStockValue: (j['totalStockValue'] ?? 0).toDouble(),
      );
}
