class StockTransferItem {
  final String productId;
  final String productName;
  final String sku;
  final double quantity;
  final double unitCost;

  StockTransferItem({
    required this.productId,
    required this.productName,
    required this.sku,
    required this.quantity,
    required this.unitCost,
  });

  factory StockTransferItem.fromJson(Map<String, dynamic> j) => StockTransferItem(
        productId: j['productId'],
        productName: j['productName'] ?? '',
        sku: j['sku'] ?? '',
        quantity: (j['quantity'] ?? 0).toDouble(),
        unitCost: (j['unitCost'] ?? 0).toDouble(),
      );
}

class StockTransfer {
  final String id;
  final String transferNumber;
  final String fromWarehouseId;
  final String fromWarehouseName;
  final String toWarehouseId;
  final String toWarehouseName;
  final DateTime transferDate;
  final bool isCompleted;
  final String? notes;
  final List<StockTransferItem> items;
  final double totalQuantity;
  final double totalValue;

  StockTransfer({
    required this.id,
    required this.transferNumber,
    required this.fromWarehouseId,
    required this.fromWarehouseName,
    required this.toWarehouseId,
    required this.toWarehouseName,
    required this.transferDate,
    required this.isCompleted,
    this.notes,
    required this.items,
    required this.totalQuantity,
    required this.totalValue,
  });

  factory StockTransfer.fromJson(Map<String, dynamic> j) => StockTransfer(
        id: j['id'],
        transferNumber: j['transferNumber'] ?? '',
        fromWarehouseId: j['fromWarehouseId'],
        fromWarehouseName: j['fromWarehouseName'] ?? '',
        toWarehouseId: j['toWarehouseId'],
        toWarehouseName: j['toWarehouseName'] ?? '',
        transferDate: DateTime.parse(j['transferDate']),
        isCompleted: j['isCompleted'] ?? false,
        notes: j['notes'],
        items: ((j['items'] ?? []) as List)
            .map((e) => StockTransferItem.fromJson(e))
            .toList(),
        totalQuantity: (j['totalQuantity'] ?? 0).toDouble(),
        totalValue: (j['totalValue'] ?? 0).toDouble(),
      );
}
