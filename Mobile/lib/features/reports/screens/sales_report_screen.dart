import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/utils/money.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/kpi_tile.dart';
import '../../../core/widgets/loading_shimmer.dart';
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

  static final _df = DateFormat('yyyy-MM-dd');

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
        if (isFrom) {
          _from = picked;
        } else {
          _to = picked;
        }
      });
      _load();
    }
  }

  void _applyPreset(int days) {
    setState(() {
      _to = DateTime.now();
      _from = _to.subtract(Duration(days: days));
    });
    _load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('تقرير المبيعات'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _load),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.all(12),
          children: [
            // Date range picker card
            Card(
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Column(
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: OutlinedButton.icon(
                            icon: const Icon(Icons.calendar_today, size: 16),
                            label: Text('من: ${_df.format(_from)}',
                                style: const TextStyle(fontSize: 13)),
                            onPressed: () => _pick(true),
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: OutlinedButton.icon(
                            icon: const Icon(Icons.calendar_today, size: 16),
                            label: Text('إلى: ${_df.format(_to)}',
                                style: const TextStyle(fontSize: 13)),
                            onPressed: () => _pick(false),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    SingleChildScrollView(
                      scrollDirection: Axis.horizontal,
                      child: Row(
                        children: [
                          _preset('اليوم', 0),
                          _preset('7 أيام', 7),
                          _preset('30 يوم', 30),
                          _preset('90 يوم', 90),
                          _preset('سنة', 365),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 12),

            if (_report != null) ...[
              // KPI Tiles
              Row(
                children: [
                  Expanded(
                    child: KpiTile(
                      label: 'إجمالي المبيعات',
                      value: Money.format(_report!.totalGross),
                      icon: Icons.attach_money,
                      color: AppTheme.success,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: KpiTile(
                      label: 'الفواتير',
                      value: _report!.totalInvoices.toString(),
                      icon: Icons.receipt_long,
                      color: AppTheme.primary,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: KpiTile(
                      label: 'الربح',
                      value: Money.format(_report!.totalProfit),
                      icon: Icons.trending_up,
                      color: AppTheme.accent,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: KpiTile(
                      label: 'الضريبة',
                      value: Money.format(_report!.totalVat),
                      icon: Icons.percent,
                      color: AppTheme.warn,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
            ],

            // Daily rows
            if (_loading)
              const LoadingShimmerList(itemCount: 6, itemHeight: 64)
            else if (_report == null || _report!.rows.isEmpty)
              const Padding(
                padding: EdgeInsets.only(top: 40),
                child: EmptyState(
                  icon: Icons.bar_chart,
                  message: 'لا توجد بيانات للفترة المحددة',
                ),
              )
            else ...[
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 6),
                child: Text(
                  'تفاصيل يومية',
                  style: TextStyle(
                    fontSize: 13,
                    color: Theme.of(context).colorScheme.onSurface.withOpacity(0.6),
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              ..._report!.rows.map((r) => Padding(
                    padding: const EdgeInsets.only(bottom: 6),
                    child: Card(
                      child: ListTile(
                        leading: Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: AppTheme.primary.withOpacity(0.1),
                            borderRadius: BorderRadius.circular(10),
                          ),
                          child: const Icon(Icons.calendar_today,
                              color: AppTheme.primary, size: 18),
                        ),
                        title: Text(_df.format(r.date),
                            style: const TextStyle(fontWeight: FontWeight.w600)),
                        subtitle: Text(
                          '${r.invoiceCount} فاتورة',
                          style: const TextStyle(fontSize: 12),
                        ),
                        trailing: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          crossAxisAlignment: CrossAxisAlignment.end,
                          children: [
                            Text(Money.format(r.totalSales),
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold, fontSize: 14)),
                            Text('ربح ${Money.format(r.profit)}',
                                style: const TextStyle(
                                    fontSize: 11, color: Colors.green)),
                          ],
                        ),
                      ),
                    ),
                  )),
            ],
          ],
        ),
      ),
    );
  }

  Widget _preset(String label, int days) {
    final selected = (DateTime.now().difference(_from).inDays - days).abs() < 2;
    return Padding(
      padding: const EdgeInsets.only(left: 6),
      child: ChoiceChip(
        label: Text(label, style: const TextStyle(fontSize: 12)),
        selected: selected,
        onSelected: (_) => _applyPreset(days),
      ),
    );
  }
}
