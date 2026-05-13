import '../../../core/api/api_client.dart';
import '../../../core/config/api_config.dart';
import '../models/dashboard_kpi.dart';
import '../models/sales_report.dart';
import '../models/top_product.dart';

class ReportService {
  static Future<DashboardKpi> getDashboard() async {
    final data = await apiClient.get('${ApiConfig.reports}/dashboard');
    return DashboardKpi.fromJson(data);
  }

  static Future<SalesReport> getSalesReport({DateTime? from, DateTime? to}) async {
    final data = await apiClient.get('${ApiConfig.reports}/sales', query: {
      if (from != null) 'from': from.toUtc().toIso8601String(),
      if (to != null) 'to': to.toUtc().toIso8601String(),
    });
    return SalesReport.fromJson(data);
  }

  static Future<List<TopProduct>> getTopProducts(
      {DateTime? from, DateTime? to, int take = 10}) async {
    final data = await apiClient.get('${ApiConfig.reports}/top-products', query: {
      if (from != null) 'from': from.toUtc().toIso8601String(),
      if (to != null) 'to': to.toUtc().toIso8601String(),
      'take': take,
    });
    return (data as List).map((e) => TopProduct.fromJson(e)).toList();
  }
}
