import 'dart:typed_data';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import 'package:printing/printing.dart';
import '../../../core/api/api_client.dart';
import '../../../core/config/api_config.dart';
import '../../../core/utils/money.dart';
import '../models/sale.dart';

class ReceiptPdfService {
  static Future<Uint8List> buildThermalReceipt(Sale sale,
      {String companyName = '', String taxRegistrationNumber = ''}) async {
    final qrBytes = await _fetchQr(sale.id);
    final arabicFont = await PdfGoogleFonts.cairoRegular();
    final arabicBold = await PdfGoogleFonts.cairoBold();

    final theme = pw.ThemeData.withFont(base: arabicFont, bold: arabicBold);
    final doc = pw.Document(theme: theme);

    doc.addPage(pw.Page(
      pageFormat: const PdfPageFormat(80 * PdfPageFormat.mm, double.infinity,
          marginAll: 4 * PdfPageFormat.mm),
      textDirection: pw.TextDirection.rtl,
      build: (ctx) => pw.Column(
        crossAxisAlignment: pw.CrossAxisAlignment.stretch,
        children: [
          if (companyName.isNotEmpty)
            pw.Center(
                child: pw.Text(companyName,
                    style: pw.TextStyle(
                        fontWeight: pw.FontWeight.bold, fontSize: 14))),
          if (taxRegistrationNumber.isNotEmpty)
            pw.Center(
                child: pw.Text('الرقم الضريبي: $taxRegistrationNumber',
                    style: const pw.TextStyle(fontSize: 9))),
          pw.Divider(borderStyle: pw.BorderStyle.dashed),
          _kv('فاتورة', sale.invoiceNumber),
          _kv('التاريخ', sale.saleDate.toLocal().toString().substring(0, 16)),
          _kv('العميل', sale.customerName ?? 'عميل نقدي'),
          if (sale.eInvoiceUuid != null)
            _kv('ETA', sale.eInvoiceUuid!, small: true),
          pw.Divider(borderStyle: pw.BorderStyle.dashed),
          ...sale.items.map((i) => pw.Padding(
                padding: const pw.EdgeInsets.symmetric(vertical: 2),
                child: pw.Column(
                  crossAxisAlignment: pw.CrossAxisAlignment.start,
                  children: [
                    pw.Text(i.productNameSnapshot,
                        style: const pw.TextStyle(fontSize: 10)),
                    pw.Row(
                      mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
                      children: [
                        pw.Text('${i.quantity} × ${i.unitPrice.toStringAsFixed(2)}',
                            style: const pw.TextStyle(fontSize: 9)),
                        pw.Text(Money.format(i.lineTotal),
                            style: pw.TextStyle(
                                fontSize: 10,
                                fontWeight: pw.FontWeight.bold)),
                      ],
                    ),
                  ],
                ),
              )),
          pw.Divider(borderStyle: pw.BorderStyle.dashed),
          _money('المجموع', sale.subTotal),
          _money('الخصم', sale.discountAmount),
          _money('الضريبة', sale.vatAmount),
          pw.Divider(),
          _money('الإجمالي', sale.total, bold: true, fontSize: 13),
          _money('المدفوع', sale.paidAmount),
          _money('الباقي', sale.changeAmount),
          pw.SizedBox(height: 8),
          if (qrBytes != null)
            pw.Center(
              child: pw.Image(pw.MemoryImage(qrBytes), width: 100, height: 100),
            ),
          pw.SizedBox(height: 4),
          pw.Center(
              child: pw.Text('شكراً لزيارتكم',
                  style: const pw.TextStyle(fontSize: 9))),
        ],
      ),
    ));

    return doc.save();
  }

  static Future<void> print(Sale sale,
      {String companyName = '', String taxRegistrationNumber = ''}) async {
    final bytes = await buildThermalReceipt(sale,
        companyName: companyName,
        taxRegistrationNumber: taxRegistrationNumber);
    await Printing.layoutPdf(onLayout: (_) async => bytes);
  }

  static Future<void> share(Sale sale,
      {String companyName = '', String taxRegistrationNumber = ''}) async {
    final bytes = await buildThermalReceipt(sale,
        companyName: companyName,
        taxRegistrationNumber: taxRegistrationNumber);
    await Printing.sharePdf(bytes: bytes, filename: '${sale.invoiceNumber}.pdf');
  }

  static Future<Uint8List?> _fetchQr(String saleId) async {
    try {
      final bytes = await apiClient.getBytes('${ApiConfig.sales}/$saleId/qr');
      return Uint8List.fromList(bytes);
    } catch (_) {
      return null;
    }
  }

  static pw.Widget _kv(String k, String v, {bool small = false}) => pw.Row(
        mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
        children: [
          pw.Text(k, style: pw.TextStyle(fontSize: small ? 9 : 10)),
          pw.Flexible(
              child: pw.Text(v,
                  style: pw.TextStyle(fontSize: small ? 8 : 10),
                  textAlign: pw.TextAlign.left)),
        ],
      );

  static pw.Widget _money(String label, double amount,
          {bool bold = false, double fontSize = 11}) =>
      pw.Row(
        mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
        children: [
          pw.Text(label,
              style: pw.TextStyle(
                  fontSize: fontSize,
                  fontWeight: bold ? pw.FontWeight.bold : pw.FontWeight.normal)),
          pw.Text(Money.format(amount),
              style: pw.TextStyle(
                  fontSize: fontSize,
                  fontWeight: bold ? pw.FontWeight.bold : pw.FontWeight.normal)),
        ],
      );
}
