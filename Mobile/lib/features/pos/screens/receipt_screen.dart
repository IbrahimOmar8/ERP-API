import 'package:flutter/material.dart';
import '../../../core/utils/money.dart';
import '../models/sale.dart';
import '../services/pos_service.dart';

class ReceiptScreen extends StatefulWidget {
  final Sale sale;
  const ReceiptScreen({super.key, required this.sale});

  @override
  State<ReceiptScreen> createState() => _ReceiptScreenState();
}

class _ReceiptScreenState extends State<ReceiptScreen> {
  bool _etaSubmitting = false;
  Sale? _sale;

  @override
  void initState() {
    super.initState();
    _sale = widget.sale;
  }

  Future<void> _submitEta() async {
    setState(() => _etaSubmitting = true);
    try {
      final updated = await PosService.submitEta(_sale!.id);
      if (updated != null) setState(() => _sale = updated);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('تم الإرسال للمصلحة')));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _etaSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final sale = _sale!;
    return Scaffold(
      appBar: AppBar(
        title: const Text('الفاتورة'),
        actions: [
          IconButton(
              icon: const Icon(Icons.home),
              onPressed: () => Navigator.of(context).popUntil((r) => r.isFirst)),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Card(
            color: const Color(0xFFE8F5E9),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  const Icon(Icons.check_circle, size: 60, color: Colors.green),
                  const SizedBox(height: 8),
                  Text('فاتورة رقم ${sale.invoiceNumber}',
                      style: const TextStyle(
                          fontSize: 18, fontWeight: FontWeight.bold)),
                  Text('${sale.saleDate.toLocal()}'),
                  if (sale.eInvoiceUuid != null) ...[
                    const SizedBox(height: 8),
                    Text('ETA UUID: ${sale.eInvoiceUuid}',
                        style: const TextStyle(fontSize: 11, color: Colors.grey)),
                  ],
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('الأصناف',
                      style: TextStyle(fontWeight: FontWeight.bold)),
                  const Divider(),
                  ...sale.items.map((i) => Padding(
                        padding: const EdgeInsets.symmetric(vertical: 6),
                        child: Row(
                          children: [
                            Expanded(
                                child: Text(
                                    '${i.productNameSnapshot} × ${i.quantity}')),
                            Text(Money.format(i.lineTotal)),
                          ],
                        ),
                      )),
                  const Divider(),
                  _row('المجموع', Money.format(sale.subTotal)),
                  _row('الخصم', Money.format(sale.discountAmount)),
                  _row('ضريبة 14%', Money.format(sale.vatAmount)),
                  const Divider(),
                  _row('الإجمالي', Money.format(sale.total), bold: true),
                  _row('المدفوع', Money.format(sale.paidAmount)),
                  _row('الباقي', Money.format(sale.changeAmount)),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          if (sale.eInvoiceUuid == null)
            ElevatedButton.icon(
              onPressed: _etaSubmitting ? null : _submitEta,
              icon: const Icon(Icons.cloud_upload),
              label: _etaSubmitting
                  ? const Text('جاري الإرسال...')
                  : const Text('إرسال للمصلحة (ETA)'),
            ),
        ],
      ),
    );
  }

  Widget _row(String label, String value, {bool bold = false}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label,
                style: TextStyle(
                    fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
            Text(value,
                style: TextStyle(
                    fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
          ],
        ),
      );
}
