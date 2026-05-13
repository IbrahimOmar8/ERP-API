class SalesReportRow {
  final DateTime date;
  final int invoiceCount;
  final double netSales;
  final double vatAmount;
  final double totalSales;
  final double profit;

  SalesReportRow({
    required this.date,
    required this.invoiceCount,
    required this.netSales,
    required this.vatAmount,
    required this.totalSales,
    required this.profit,
  });

  factory SalesReportRow.fromJson(Map<String, dynamic> j) => SalesReportRow(
        date: DateTime.parse(j['date']),
        invoiceCount: j['invoiceCount'] ?? 0,
        netSales: (j['netSales'] ?? 0).toDouble(),
        vatAmount: (j['vatAmount'] ?? 0).toDouble(),
        totalSales: (j['totalSales'] ?? 0).toDouble(),
        profit: (j['profit'] ?? 0).toDouble(),
      );
}

class SalesReport {
  final DateTime from;
  final DateTime to;
  final List<SalesReportRow> rows;
  final double totalNetSales;
  final double totalVat;
  final double totalGross;
  final double totalProfit;
  final int totalInvoices;

  SalesReport({
    required this.from,
    required this.to,
    required this.rows,
    required this.totalNetSales,
    required this.totalVat,
    required this.totalGross,
    required this.totalProfit,
    required this.totalInvoices,
  });

  factory SalesReport.fromJson(Map<String, dynamic> j) => SalesReport(
        from: DateTime.parse(j['from']),
        to: DateTime.parse(j['to']),
        rows: ((j['rows'] ?? []) as List)
            .map((e) => SalesReportRow.fromJson(e))
            .toList(),
        totalNetSales: (j['totalNetSales'] ?? 0).toDouble(),
        totalVat: (j['totalVat'] ?? 0).toDouble(),
        totalGross: (j['totalGross'] ?? 0).toDouble(),
        totalProfit: (j['totalProfit'] ?? 0).toDouble(),
        totalInvoices: j['totalInvoices'] ?? 0,
      );
}
