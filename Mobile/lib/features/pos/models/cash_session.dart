class CashSession {
  final String id;
  final String cashRegisterId;
  final String? cashRegisterName;
  final String warehouseId;
  final String? warehouseName;
  final String cashierUserId;
  final DateTime openedAt;
  final DateTime? closedAt;
  final double openingBalance;
  final double closingBalance;
  final double expectedBalance;
  final double totalCashSales;
  final double totalCardSales;
  final int status; // 1 = open, 2 = closed

  CashSession({
    required this.id,
    required this.cashRegisterId,
    this.cashRegisterName,
    required this.warehouseId,
    this.warehouseName,
    required this.cashierUserId,
    required this.openedAt,
    this.closedAt,
    required this.openingBalance,
    required this.closingBalance,
    required this.expectedBalance,
    required this.totalCashSales,
    required this.totalCardSales,
    required this.status,
  });

  factory CashSession.fromJson(Map<String, dynamic> json) => CashSession(
        id: json['id'],
        cashRegisterId: json['cashRegisterId'],
        cashRegisterName: json['cashRegisterName'],
        warehouseId: json['warehouseId'] ?? '',
        warehouseName: json['warehouseName'],
        cashierUserId: json['cashierUserId'],
        openedAt: DateTime.parse(json['openedAt']),
        closedAt: json['closedAt'] != null ? DateTime.parse(json['closedAt']) : null,
        openingBalance: (json['openingBalance'] ?? 0).toDouble(),
        closingBalance: (json['closingBalance'] ?? 0).toDouble(),
        expectedBalance: (json['expectedBalance'] ?? 0).toDouble(),
        totalCashSales: (json['totalCashSales'] ?? 0).toDouble(),
        totalCardSales: (json['totalCardSales'] ?? 0).toDouble(),
        status: json['status'] ?? 1,
      );
}

class CashRegister {
  final String id;
  final String name;
  final String code;
  final String warehouseId;
  final String? warehouseName;
  final bool isActive;
  final bool hasOpenSession;

  CashRegister({
    required this.id,
    required this.name,
    required this.code,
    required this.warehouseId,
    this.warehouseName,
    required this.isActive,
    required this.hasOpenSession,
  });

  factory CashRegister.fromJson(Map<String, dynamic> json) => CashRegister(
        id: json['id'],
        name: json['name'] ?? '',
        code: json['code'] ?? '',
        warehouseId: json['warehouseId'],
        warehouseName: json['warehouseName'],
        isActive: json['isActive'] ?? true,
        hasOpenSession: json['hasOpenSession'] ?? false,
      );
}
