import '../../../core/api/api_client.dart';
import '../../../core/config/api_config.dart';
import '../models/cash_session.dart';
import '../models/customer.dart';
import '../models/sale.dart';

class PosService {
  // Customers
  static Future<List<Customer>> getCustomers({String? search}) async {
    final data =
        await apiClient.get(ApiConfig.customers, query: {'search': search});
    return (data as List).map((e) => Customer.fromJson(e)).toList();
  }

  static Future<Customer> createCustomer(Map<String, dynamic> dto) async {
    final data = await apiClient.post(ApiConfig.customers, dto);
    return Customer.fromJson(data);
  }

  // Cash registers
  static Future<List<CashRegister>> getCashRegisters() async {
    final data = await apiClient.get(ApiConfig.cashRegisters);
    return (data as List).map((e) => CashRegister.fromJson(e)).toList();
  }

  // Sessions
  static Future<CashSession?> getCurrentSession(String userId) async {
    try {
      final data = await apiClient.get('${ApiConfig.cashSessions}/current/$userId');
      return data == null ? null : CashSession.fromJson(data);
    } on ApiException catch (e) {
      if (e.statusCode == 404) return null;
      rethrow;
    }
  }

  static Future<CashSession> openSession(
      String userId, String registerId, double openingBalance) async {
    final data = await apiClient.post(
      '${ApiConfig.cashSessions}/open/$userId',
      {
        'cashRegisterId': registerId,
        'openingBalance': openingBalance,
      },
    );
    return CashSession.fromJson(data);
  }

  static Future<CashSession> closeSession(
      String sessionId, double closingBalance, {String? notes}) async {
    final data = await apiClient.post(
      '${ApiConfig.cashSessions}/$sessionId/close',
      {'closingBalance': closingBalance, 'notes': notes},
    );
    return CashSession.fromJson(data);
  }

  // Sales
  static Future<Sale> createSale({
    required String cashierUserId,
    String? customerId,
    required String warehouseId,
    required String cashSessionId,
    required List<Map<String, dynamic>> items,
    required List<Map<String, dynamic>> payments,
    double discountAmount = 0,
    double discountPercent = 0,
    String? notes,
  }) async {
    final data = await apiClient.post('${ApiConfig.sales}/$cashierUserId', {
      'customerId': customerId,
      'warehouseId': warehouseId,
      'cashSessionId': cashSessionId,
      'items': items,
      'payments': payments,
      'discountAmount': discountAmount,
      'discountPercent': discountPercent,
      'notes': notes,
    });
    return Sale.fromJson(data);
  }

  static Future<List<Sale>> getSales({String? cashierId}) async {
    final data = await apiClient.get(ApiConfig.sales,
        query: {'cashierUserId': cashierId, 'pageSize': 100});
    return (data as List).map((e) => Sale.fromJson(e)).toList();
  }

  static Future<Sale?> submitEta(String saleId) async {
    final data = await apiClient.post('${ApiConfig.sales}/$saleId/submit-eta', {});
    return data == null ? null : Sale.fromJson(data);
  }

  static Future<Sale?> refund(String saleId, String reason, String userId) async {
    final data = await apiClient.post(
      '${ApiConfig.sales}/$saleId/refund',
      {'reason': reason, 'userId': userId},
    );
    return data == null ? null : Sale.fromJson(data);
  }
}
