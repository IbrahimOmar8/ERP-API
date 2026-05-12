import '../../../core/api/api_client.dart';
import '../../../core/config/api_config.dart';
import '../models/category.dart';
import '../models/product.dart';
import '../models/stock_item.dart';
import '../models/warehouse.dart';

class InventoryService {
  static Future<List<Product>> getProducts({String? search, String? categoryId}) async {
    final data = await apiClient.get(ApiConfig.products, query: {
      'search': search,
      'categoryId': categoryId,
      'pageNumber': 1,
      'pageSize': 100,
    });
    return (data as List).map((e) => Product.fromJson(e)).toList();
  }

  static Future<Product?> getByBarcode(String barcode) async {
    try {
      final data = await apiClient.get('${ApiConfig.products}/barcode/$barcode');
      return data == null ? null : Product.fromJson(data);
    } on ApiException catch (e) {
      if (e.statusCode == 404) return null;
      rethrow;
    }
  }

  static Future<Product> createProduct(Map<String, dynamic> dto) async {
    final data = await apiClient.post(ApiConfig.products, dto);
    return Product.fromJson(data);
  }

  static Future<List<Warehouse>> getWarehouses() async {
    final data = await apiClient.get(ApiConfig.warehouses);
    return (data as List).map((e) => Warehouse.fromJson(e)).toList();
  }

  static Future<List<Category>> getCategories() async {
    final data = await apiClient.get(ApiConfig.categories);
    return (data as List).map((e) => Category.fromJson(e)).toList();
  }

  static Future<List<StockItem>> getStockByWarehouse(String warehouseId) async {
    final data = await apiClient.get('${ApiConfig.stock}/warehouse/$warehouseId');
    return (data as List).map((e) => StockItem.fromJson(e)).toList();
  }

  static Future<void> adjustStock({
    required String productId,
    required String warehouseId,
    required double newQuantity,
    String? reason,
  }) async {
    await apiClient.post('${ApiConfig.stock}/adjust', {
      'productId': productId,
      'warehouseId': warehouseId,
      'newQuantity': newQuantity,
      'reason': reason,
    });
  }
}
