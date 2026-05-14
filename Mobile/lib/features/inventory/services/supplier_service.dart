import '../../../core/api/api_client.dart';
import '../../../core/config/api_config.dart';
import '../models/supplier.dart';

class SupplierService {
  static Future<List<Supplier>> getAll() async {
    final data = await apiClient.get(ApiConfig.suppliers);
    return (data as List).map((e) => Supplier.fromJson(e)).toList();
  }

  static Future<Supplier> create({
    required String name,
    String? phone,
    String? email,
    String? address,
    String? taxRegistrationNumber,
  }) async {
    final data = await apiClient.post(ApiConfig.suppliers, {
      'name': name,
      'phone': phone,
      'email': email,
      'address': address,
      'taxRegistrationNumber': taxRegistrationNumber,
    });
    return Supplier.fromJson(data);
  }
}
