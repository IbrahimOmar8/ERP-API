class SaleItemSnapshot {
  final String productId;
  final String productNameSnapshot;
  final double quantity;
  final double unitPrice;
  final double vatRate;
  final double vatAmount;
  final double lineTotal;

  SaleItemSnapshot({
    required this.productId,
    required this.productNameSnapshot,
    required this.quantity,
    required this.unitPrice,
    required this.vatRate,
    required this.vatAmount,
    required this.lineTotal,
  });

  factory SaleItemSnapshot.fromJson(Map<String, dynamic> json) => SaleItemSnapshot(
        productId: json['productId'],
        productNameSnapshot: json['productNameSnapshot'] ?? '',
        quantity: (json['quantity'] ?? 0).toDouble(),
        unitPrice: (json['unitPrice'] ?? 0).toDouble(),
        vatRate: (json['vatRate'] ?? 0).toDouble(),
        vatAmount: (json['vatAmount'] ?? 0).toDouble(),
        lineTotal: (json['lineTotal'] ?? 0).toDouble(),
      );
}

class Sale {
  final String id;
  final String invoiceNumber;
  final String? customerId;
  final String? customerName;
  final DateTime saleDate;
  final double subTotal;
  final double discountAmount;
  final double vatAmount;
  final double total;
  final double paidAmount;
  final double changeAmount;
  final int status;
  final String? eInvoiceUuid;
  final int? eInvoiceStatus;
  final List<SaleItemSnapshot> items;

  Sale({
    required this.id,
    required this.invoiceNumber,
    this.customerId,
    this.customerName,
    required this.saleDate,
    required this.subTotal,
    required this.discountAmount,
    required this.vatAmount,
    required this.total,
    required this.paidAmount,
    required this.changeAmount,
    required this.status,
    this.eInvoiceUuid,
    this.eInvoiceStatus,
    required this.items,
  });

  factory Sale.fromJson(Map<String, dynamic> json) => Sale(
        id: json['id'],
        invoiceNumber: json['invoiceNumber'] ?? '',
        customerId: json['customerId'],
        customerName: json['customerName'],
        saleDate: DateTime.parse(json['saleDate']),
        subTotal: (json['subTotal'] ?? 0).toDouble(),
        discountAmount: (json['discountAmount'] ?? 0).toDouble(),
        vatAmount: (json['vatAmount'] ?? 0).toDouble(),
        total: (json['total'] ?? 0).toDouble(),
        paidAmount: (json['paidAmount'] ?? 0).toDouble(),
        changeAmount: (json['changeAmount'] ?? 0).toDouble(),
        status: json['status'] ?? 0,
        eInvoiceUuid: json['eInvoiceUuid'],
        eInvoiceStatus: json['eInvoiceStatus'],
        items: ((json['items'] ?? []) as List)
            .map((e) => SaleItemSnapshot.fromJson(e))
            .toList(),
      );
}
