class Product {
  final String id;
  final String sku;
  final String? barcode;
  final String nameAr;
  final String? nameEn;
  final String? description;
  final String categoryId;
  final String? categoryName;
  final String unitId;
  final String? unitName;
  final double purchasePrice;
  final double salePrice;
  final double minSalePrice;
  final double vatRate;
  final double currentStock;
  final bool trackStock;
  final bool isActive;

  Product({
    required this.id,
    required this.sku,
    this.barcode,
    required this.nameAr,
    this.nameEn,
    this.description,
    required this.categoryId,
    this.categoryName,
    required this.unitId,
    this.unitName,
    required this.purchasePrice,
    required this.salePrice,
    required this.minSalePrice,
    required this.vatRate,
    required this.currentStock,
    required this.trackStock,
    required this.isActive,
  });

  factory Product.fromJson(Map<String, dynamic> json) => Product(
        id: json['id'],
        sku: json['sku'] ?? '',
        barcode: json['barcode'],
        nameAr: json['nameAr'] ?? '',
        nameEn: json['nameEn'],
        description: json['description'],
        categoryId: json['categoryId'],
        categoryName: json['categoryName'],
        unitId: json['unitId'],
        unitName: json['unitName'],
        purchasePrice: (json['purchasePrice'] ?? 0).toDouble(),
        salePrice: (json['salePrice'] ?? 0).toDouble(),
        minSalePrice: (json['minSalePrice'] ?? 0).toDouble(),
        vatRate: (json['vatRate'] ?? 14).toDouble(),
        currentStock: (json['currentStock'] ?? 0).toDouble(),
        trackStock: json['trackStock'] ?? true,
        isActive: json['isActive'] ?? true,
      );
}
