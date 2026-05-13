import 'package:flutter/material.dart';
import '../../../core/utils/money.dart';
import '../models/einvoice_submission.dart';
import '../models/sale.dart';
import '../services/eta_service.dart';
import '../services/receipt_pdf_service.dart';

class ReceiptScreen extends StatefulWidget {
  final Sale sale;
  const ReceiptScreen({super.key, required this.sale});

  @override
  State<ReceiptScreen> createState() => _ReceiptScreenState();
}

class _ReceiptScreenState extends State<ReceiptScreen> {
  bool _busy = false;
  Sale? _sale;
  EInvoiceSubmission? _submission;

  @override
  void initState() {
    super.initState();
    _sale = widget.sale;
    _loadLatestSubmission();
  }

  Future<void> _loadLatestSubmission() async {
    try {
      final s = await EtaService.getLatest(_sale!.id);
      if (mounted && s != null) setState(() => _submission = s);
    } catch (_) {/* silent: status panel falls back to sale fields */}
  }

  Future<void> _runEta(Future<EInvoiceSubmission> Function() action,
      {required String successMsg}) async {
    setState(() => _busy = true);
    try {
      final result = await action();
      if (!mounted) return;
      setState(() => _submission = result);
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(successMsg)));
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _cancelEta() async {
    final reason = await _askReason();
    if (reason == null || reason.trim().isEmpty) return;
    await _runEta(
      () => EtaService.cancel(_sale!.id, reason),
      successMsg: 'تم إلغاء الفاتورة من المصلحة',
    );
  }

  Future<String?> _askReason() async {
    final controller = TextEditingController();
    return showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('سبب الإلغاء'),
        content: TextField(
          controller: controller,
          autofocus: true,
          decoration: const InputDecoration(hintText: 'اكتب السبب'),
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx), child: const Text('إلغاء')),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, controller.text),
            child: const Text('تأكيد'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final sale = _sale!;
    return Scaffold(
      appBar: AppBar(
        title: const Text('الفاتورة'),
        actions: [
          IconButton(
              icon: const Icon(Icons.print),
              tooltip: 'طباعة',
              onPressed: () => ReceiptPdfService.print(sale)),
          IconButton(
              icon: const Icon(Icons.share),
              tooltip: 'مشاركة PDF',
              onPressed: () => ReceiptPdfService.share(sale)),
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
          _buildEtaPanel(sale),
        ],
      ),
    );
  }

  Widget _buildEtaPanel(Sale sale) {
    final sub = _submission;
    final hasUuid = sub?.submissionUuid != null || sale.eInvoiceUuid != null;
    final status = sub?.status ?? sale.eInvoiceStatus;
    final statusLabel = sub?.statusLabel ?? _saleStatusLabel(status);
    final color = _statusColor(status);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.receipt_long, size: 20),
                const SizedBox(width: 8),
                const Text('الفاتورة الإلكترونية (ETA)',
                    style: TextStyle(fontWeight: FontWeight.bold)),
                const Spacer(),
                if (statusLabel.isNotEmpty)
                  Chip(
                    label: Text(statusLabel,
                        style: const TextStyle(color: Colors.white)),
                    backgroundColor: color,
                  ),
              ],
            ),
            if (hasUuid) ...[
              const SizedBox(height: 8),
              SelectableText(
                  'UUID: ${sub?.submissionUuid ?? sale.eInvoiceUuid}',
                  style: const TextStyle(fontSize: 11, color: Colors.grey)),
            ],
            if (sub?.longId != null) ...[
              SelectableText('LongId: ${sub!.longId}',
                  style: const TextStyle(fontSize: 11, color: Colors.grey)),
            ],
            if (sub?.errorMessage != null) ...[
              const SizedBox(height: 8),
              Text(sub!.errorMessage!,
                  style: const TextStyle(color: Colors.red, fontSize: 12)),
            ],
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              children: [
                if (!hasUuid)
                  ElevatedButton.icon(
                    onPressed: _busy
                        ? null
                        : () => _runEta(() => EtaService.submit(sale.id),
                            successMsg: 'تم الإرسال للمصلحة'),
                    icon: const Icon(Icons.cloud_upload),
                    label: const Text('إرسال للمصلحة'),
                  ),
                if (hasUuid)
                  OutlinedButton.icon(
                    onPressed: _busy
                        ? null
                        : () => _runEta(() => EtaService.refresh(sale.id),
                            successMsg: 'تم تحديث الحالة'),
                    icon: const Icon(Icons.refresh),
                    label: const Text('تحديث الحالة'),
                  ),
                if (hasUuid && status != 5 /* Cancelled */)
                  OutlinedButton.icon(
                    onPressed: _busy ? null : _cancelEta,
                    icon: const Icon(Icons.cancel, color: Colors.red),
                    label: const Text('إلغاء من المصلحة',
                        style: TextStyle(color: Colors.red)),
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  String _saleStatusLabel(int? s) => switch (s) {
        0 => 'قيد الإرسال',
        1 => 'تم التقديم',
        2 => 'مقبولة',
        3 => 'غير صالحة',
        4 => 'مرفوضة',
        5 => 'ملغاة',
        _ => '',
      };

  Color _statusColor(int? s) => switch (s) {
        2 => Colors.green,
        1 => Colors.blue,
        3 || 4 => Colors.red,
        5 => Colors.grey,
        _ => Colors.orange,
      };

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
