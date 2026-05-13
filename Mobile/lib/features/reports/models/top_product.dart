class TopProduct {
  final String productId;
  final String productName;
  final double quantitySold;
  final double revenue;
  final double profit;

  TopProduct({
    required this.productId,
    required this.productName,
    required this.quantitySold,
    required this.revenue,
    required this.profit,
  });

  factory TopProduct.fromJson(Map<String, dynamic> j) => TopProduct(
        productId: j['productId'],
        productName: j['productName'] ?? '',
        quantitySold: (j['quantitySold'] ?? 0).toDouble(),
        revenue: (j['revenue'] ?? 0).toDouble(),
        profit: (j['profit'] ?? 0).toDouble(),
      );
}
