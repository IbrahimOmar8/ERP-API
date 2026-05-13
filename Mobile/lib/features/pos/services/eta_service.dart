import '../../../core/api/api_client.dart';
import '../../../core/config/api_config.dart';
import '../models/einvoice_submission.dart';

class EtaService {
  static Future<EInvoiceSubmission> submit(String saleId,
      {String? signedCmsBase64}) async {
    final data = await apiClient.post(
      '${ApiConfig.eInvoice}/sales/$saleId/submit',
      {'signedCmsBase64': signedCmsBase64},
    );
    return EInvoiceSubmission.fromJson(data);
  }

  static Future<EInvoiceSubmission> refresh(String saleId) async {
    final data =
        await apiClient.post('${ApiConfig.eInvoice}/sales/$saleId/refresh', {});
    return EInvoiceSubmission.fromJson(data);
  }

  static Future<EInvoiceSubmission> cancel(String saleId, String reason) async {
    final data = await apiClient.post(
      '${ApiConfig.eInvoice}/sales/$saleId/cancel',
      {'reason': reason},
    );
    return EInvoiceSubmission.fromJson(data);
  }

  static Future<EInvoiceSubmission?> getLatest(String saleId) async {
    try {
      final data = await apiClient.get('${ApiConfig.eInvoice}/sales/$saleId');
      return data == null ? null : EInvoiceSubmission.fromJson(data);
    } on ApiException catch (e) {
      if (e.statusCode == 404) return null;
      rethrow;
    }
  }
}
