import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../core/utils/money.dart';
import '../models/sales_report.dart';
import '../services/report_service.dart';

class SalesReportScreen extends StatefulWidget {
  const SalesReportScreen({super.key});

  @override
  State<SalesReportScreen> createState() => _SalesReportScreenState();
}

class _SalesReportScreenState extends State<SalesReportScreen> {
  DateTime _from = DateTime.now().subtract(const Duration(days: 30));
  DateTime _to = DateTime.now();
  SalesReport? _report;
  bool _loading = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      _report = await ReportService.getSalesReport(from: _from, to: _to);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _pick(bool isFrom) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: isFrom ? _from : _to,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 1)),
    );
    if (picked != null) {
      setState(() {
        if (isFrom) _from = picked;
        else _to = picked;
      });
      _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    final df = DateFormat('yyyy-MM-dd');
    return Scaffold(
      appBar: AppBar(
        title: const Text('تقرير المبيعات'),
        actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: _load)],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    icon: const Icon(Icons.calendar_today),
                    label: Text('من: ${df.format(_from)}'),
                    onPressed: () => _pick(true),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: OutlinedButton.icon(
                    icon: const Icon(Icons.calendar_today),
                    label: Text('إلى: ${df.format(_to)}'),
                    onPressed: () => _pick(false),
                  ),
                ),
              ],
            ),
          ),
          if (_report != null)
            Card(
              margin: const EdgeInsets.symmetric(horizontal: 12),
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Column(
                  children: [
                    _kv('عدد الفواتير', _report!.totalInvoices.toString()),
                    _kv('صافي المبيعات', Money.format(_report!.totalNetSales)),
                    _kv('الضريبة', Money.format(_report!.totalVat)),
                    _kv('إجمالي المبيعات', Money.format(_report!.totalGross),
                        bold: true),
                    _kv('الربح', Money.format(_report!.totalProfit), bold: true),
                  ],
                ),
              ),
            ),
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator())
                : _report == null || _report!.rows.isEmpty
                    ? const Center(child: Text('لا توجد بيانات'))
                    : ListView.builder(
                        itemCount: _report!.rows.length,
                        itemBuilder: (_, i) {
                          final r = _report!.rows[i];
                          return ListTile(
                            leading: const Icon(Icons.calendar_today),
                            title: Text(df.format(r.date)),
                            subtitle: Text('${r.invoiceCount} فاتورة'),
                            trailing: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              crossAxisAlignment: CrossAxisAlignment.end,
                              children: [
                                Text(Money.format(r.totalSales),
                                    style: const TextStyle(
                                        fontWeight: FontWeight.bold)),
                                Text('ربح ${Money.format(r.profit)}',
                                    style: const TextStyle(
                                        fontSize: 11, color: Colors.green)),
                              ],
                            ),
                          );
                        }),
          ),
        ],
      ),
    );
  }

  Widget _kv(String k, String v, {bool bold = false}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 2),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(k,
                style: TextStyle(
                    fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
            Text(v,
                style: TextStyle(
                    fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
          ],
        ),
      );
}
