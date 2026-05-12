class StockItem {
  final String id;
  final String productId;
  final String productName;
  final String sku;
  final String warehouseId;
  final String warehouseName;
  final double quantity;
  final double availableQuantity;
  final double averageCost;

  StockItem({
    required this.id,
    required this.productId,
    required this.productName,
    required this.sku,
    required this.warehouseId,
    required this.warehouseName,
    required this.quantity,
    required this.availableQuantity,
    required this.averageCost,
  });

  factory StockItem.fromJson(Map<String, dynamic> json) => StockItem(
        id: json['id'],
        productId: json['productId'],
        productName: json['productName'] ?? '',
        sku: json['sku'] ?? '',
        warehouseId: json['warehouseId'],
        warehouseName: json['warehouseName'] ?? '',
        quantity: (json['quantity'] ?? 0).toDouble(),
        availableQuantity: (json['availableQuantity'] ?? 0).toDouble(),
        averageCost: (json['averageCost'] ?? 0).toDouble(),
      );
}
