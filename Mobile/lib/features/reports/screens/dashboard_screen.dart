import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

import '../../../core/utils/money.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/kpi_tile.dart';
import '../../../core/widgets/loading_shimmer.dart';
import '../../../core/widgets/section_header.dart';
import '../models/dashboard_kpi.dart';
import '../models/sales_report.dart';
import '../models/top_product.dart';
import '../services/report_service.dart';
import 'sales_report_screen.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  DashboardKpi? _kpi;
  List<TopProduct> _top = [];
  SalesReport? _trend;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final now = DateTime.now();
      final from = now.subtract(const Duration(days: 29));
      final results = await Future.wait([
        ReportService.getDashboard(),
        ReportService.getTopProducts(take: 5),
        ReportService.getSalesReport(from: from, to: now),
      ]);
      _kpi = results[0] as DashboardKpi;
      _top = results[1] as List<TopProduct>;
      _trend = results[2] as SalesReport;
    } catch (e) {
      _error = e.toString();
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('لوحة التحكم'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _load),
        ],
      ),
      body: _loading
          ? const LoadingShimmerList(itemCount: 6, itemHeight: 84)
          : _error != null
              ? EmptyState(
                  icon: Icons.cloud_off,
                  message: 'تعذر التحميل: $_error',
                  actionLabel: 'إعادة المحاولة',
                  onAction: _load,
                )
              : _kpi == null
                  ? const EmptyState(
                      icon: Icons.inbox,
                      message: 'لا توجد بيانات بعد')
                  : RefreshIndicator(
                      onRefresh: _load,
                      child: ListView(
                        padding: const EdgeInsets.all(12),
                        children: [
                          const SectionHeader(title: 'اليوم'),
                          _kpiRow([
                            KpiTile(
                                label: 'مبيعات اليوم',
                                value: Money.format(_kpi!.todaySales),
                                icon: Icons.attach_money,
                                color: Colors.green),
                            KpiTile(
                                label: 'فواتير اليوم',
                                value: _kpi!.todayInvoiceCount.toString(),
                                icon: Icons.receipt,
                                color: Colors.blue),
                          ]),
                          const SizedBox(height: 8),
                          _kpiRow([
                            KpiTile(
                                label: 'ربح اليوم',
                                value: Money.format(_kpi!.todayProfit),
                                icon: Icons.trending_up,
                                color: Colors.teal),
                            KpiTile(
                                label: 'جلسات مفتوحة',
                                value: _kpi!.openSessionCount.toString(),
                                icon: Icons.point_of_sale,
                                color: Colors.orange),
                          ]),
                          const SizedBox(height: 16),
                          const SectionHeader(title: 'الشهر'),
                          _kpiRow([
                            KpiTile(
                                label: 'مبيعات الشهر',
                                value: Money.format(_kpi!.monthSales),
                                icon: Icons.attach_money,
                                color: Colors.green),
                            KpiTile(
                                label: 'فواتير الشهر',
                                value: _kpi!.monthInvoiceCount.toString(),
                                icon: Icons.receipt_long,
                                color: Colors.blue),
                          ]),
                          const SizedBox(height: 8),
                          _kpiRow([
                            KpiTile(
                                label: 'ربح الشهر',
                                value: Money.format(_kpi!.monthProfit),
                                icon: Icons.trending_up,
                                color: Colors.teal),
                            KpiTile(
                                label: 'قيمة المخزون',
                                value: Money.format(_kpi!.totalStockValue),
                                icon: Icons.warehouse,
                                color: Colors.indigo),
                          ]),
                          const SizedBox(height: 16),
                          _TrendChartCard(report: _trend),
                          const SizedBox(height: 16),
                          const SectionHeader(title: 'عام'),
                          _kpiRow([
                            KpiTile(
                                label: 'العملاء',
                                value: _kpi!.customerCount.toString(),
                                icon: Icons.people,
                                color: Colors.purple),
                            KpiTile(
                                label: 'الأصناف',
                                value: _kpi!.productCount.toString(),
                                icon: Icons.inventory_2,
                                color: Colors.deepOrange),
                          ]),
                          const SizedBox(height: 8),
                          KpiTile(
                              label: 'أصناف بحد أدنى',
                              value: _kpi!.lowStockCount.toString(),
                              icon: Icons.warning,
                              color: Colors.red),
                          const SizedBox(height: 16),
                          ElevatedButton.icon(
                              icon: const Icon(Icons.analytics),
                              label: const Text('تقرير المبيعات التفصيلي'),
                              onPressed: () => Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                      builder: (_) =>
                                          const SalesReportScreen()))),
                          const SizedBox(height: 16),
                          SectionHeader(
                              title: 'الأعلى مبيعاً',
                              action: _top.isEmpty ? null : 'الكل'),
                          if (_top.isEmpty)
                            const Padding(
                              padding: EdgeInsets.symmetric(vertical: 24),
                              child: EmptyState(
                                  icon: Icons.bar_chart,
                                  message: 'لا توجد بيانات مبيعات بعد'),
                            )
                          else
                            ..._top.map((p) => Card(
                                  child: ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor:
                                          Theme.of(context).colorScheme.primary,
                                      child: const Icon(Icons.star,
                                          color: Colors.white, size: 18),
                                    ),
                                    title: Text(p.productName,
                                        style: const TextStyle(
                                            fontWeight: FontWeight.w600)),
                                    subtitle: Text('${p.quantitySold} قطعة'),
                                    trailing: Text(Money.format(p.revenue),
                                        style: const TextStyle(
                                            fontWeight: FontWeight.bold,
                                            color: Colors.green)),
                                  ),
                                )),
                        ],
                      ),
                    ),
    );
  }

  Widget _kpiRow(List<Widget> children) => Row(
        children: [
          for (var i = 0; i < children.length; i++) ...[
            if (i > 0) const SizedBox(width: 8),
            Expanded(child: children[i]),
          ],
        ],
      );
}

class _TrendChartCard extends StatelessWidget {
  final SalesReport? report;
  const _TrendChartCard({required this.report});

  @override
  Widget build(BuildContext context) {
    if (report == null || report!.rows.isEmpty) {
      return Card(
        child: Container(
          height: 200,
          padding: const EdgeInsets.all(16),
          alignment: Alignment.center,
          child: const Text('لا توجد بيانات للرسم',
              style: TextStyle(color: Colors.grey)),
        ),
      );
    }

    final maxSales = report!.rows
        .map((r) => r.totalSales)
        .reduce((a, b) => a > b ? a : b);
    final spots = <FlSpot>[];
    for (var i = 0; i < report!.rows.length; i++) {
      spots.add(FlSpot(i.toDouble(), report!.rows[i].totalSales));
    }

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('اتجاه المبيعات (آخر 30 يوم)',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
            const SizedBox(height: 12),
            SizedBox(
              height: 180,
              child: LineChart(
                LineChartData(
                  minY: 0,
                  maxY: maxSales * 1.15,
                  gridData: FlGridData(
                    show: true,
                    drawVerticalLine: false,
                    horizontalInterval: maxSales == 0 ? 1 : maxSales / 4,
                    getDrawingHorizontalLine: (_) => FlLine(
                      color: Colors.grey.shade200,
                      strokeWidth: 1,
                    ),
                  ),
                  titlesData: FlTitlesData(
                    show: true,
                    rightTitles: const AxisTitles(
                        sideTitles: SideTitles(showTitles: false)),
                    topTitles: const AxisTitles(
                        sideTitles: SideTitles(showTitles: false)),
                    leftTitles: AxisTitles(
                      sideTitles: SideTitles(
                        showTitles: true,
                        reservedSize: 44,
                        getTitlesWidget: (v, _) => Padding(
                          padding: const EdgeInsets.only(right: 4),
                          child: Text(
                            v >= 1000 ? '${(v / 1000).toStringAsFixed(0)}k' : v.toStringAsFixed(0),
                            style: const TextStyle(fontSize: 10),
                          ),
                        ),
                      ),
                    ),
                    bottomTitles: AxisTitles(
                      sideTitles: SideTitles(
                        showTitles: true,
                        reservedSize: 24,
                        interval: (report!.rows.length / 5).floorToDouble().clamp(1, double.infinity),
                        getTitlesWidget: (v, _) {
                          final i = v.toInt();
                          if (i < 0 || i >= report!.rows.length) return const SizedBox();
                          final d = report!.rows[i].date;
                          return Text(
                            '${d.day}/${d.month}',
                            style: const TextStyle(fontSize: 10),
                          );
                        },
                      ),
                    ),
                  ),
                  borderData: FlBorderData(show: false),
                  lineBarsData: [
                    LineChartBarData(
                      spots: spots,
                      isCurved: true,
                      color: Theme.of(context).colorScheme.primary,
                      barWidth: 2.5,
                      dotData: const FlDotData(show: false),
                      belowBarData: BarAreaData(
                        show: true,
                        gradient: LinearGradient(
                          begin: Alignment.topCenter,
                          end: Alignment.bottomCenter,
                          colors: [
                            Theme.of(context).colorScheme.primary.withOpacity(0.25),
                            Theme.of(context).colorScheme.primary.withOpacity(0),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
