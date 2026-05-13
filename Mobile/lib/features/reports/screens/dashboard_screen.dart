import 'package:flutter/material.dart';
import '../../../core/utils/money.dart';
import '../models/dashboard_kpi.dart';
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
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final results = await Future.wait([
        ReportService.getDashboard(),
        ReportService.getTopProducts(take: 5),
      ]);
      _kpi = results[0] as DashboardKpi;
      _top = results[1] as List<TopProduct>;
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('لوحة التحكم'),
        actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: _load)],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _kpi == null
              ? const Center(child: Text('تعذر التحميل'))
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView(
                    padding: const EdgeInsets.all(12),
                    children: [
                      _section('اليوم'),
                      Row(children: [
                        _kpiCard('مبيعات اليوم',
                            Money.format(_kpi!.todaySales), Colors.green,
                            Icons.attach_money),
                        _kpiCard('فواتير اليوم',
                            _kpi!.todayInvoiceCount.toString(), Colors.blue,
                            Icons.receipt),
                      ]),
                      Row(children: [
                        _kpiCard('ربح اليوم',
                            Money.format(_kpi!.todayProfit), Colors.teal,
                            Icons.trending_up),
                        _kpiCard('جلسات مفتوحة',
                            _kpi!.openSessionCount.toString(), Colors.orange,
                            Icons.point_of_sale),
                      ]),
                      const SizedBox(height: 8),
                      _section('الشهر'),
                      Row(children: [
                        _kpiCard('مبيعات الشهر',
                            Money.format(_kpi!.monthSales), Colors.green,
                            Icons.attach_money),
                        _kpiCard('فواتير الشهر',
                            _kpi!.monthInvoiceCount.toString(), Colors.blue,
                            Icons.receipt_long),
                      ]),
                      Row(children: [
                        _kpiCard('ربح الشهر',
                            Money.format(_kpi!.monthProfit), Colors.teal,
                            Icons.trending_up),
                        _kpiCard('قيمة المخزون',
                            Money.format(_kpi!.totalStockValue),
                            Colors.indigo, Icons.warehouse),
                      ]),
                      const SizedBox(height: 8),
                      _section('عام'),
                      Row(children: [
                        _kpiCard('العملاء', _kpi!.customerCount.toString(),
                            Colors.purple, Icons.people),
                        _kpiCard('الأصناف', _kpi!.productCount.toString(),
                            Colors.deepOrange, Icons.inventory_2),
                      ]),
                      Row(children: [
                        _kpiCard(
                            'أصناف بحد أدنى',
                            _kpi!.lowStockCount.toString(),
                            Colors.red,
                            Icons.warning),
                        const Expanded(child: SizedBox()),
                      ]),
                      const SizedBox(height: 16),
                      Row(
                        children: [
                          Expanded(
                              child: ElevatedButton.icon(
                                  icon: const Icon(Icons.analytics),
                                  label: const Text('تقرير المبيعات'),
                                  onPressed: () => Navigator.push(
                                      context,
                                      MaterialPageRoute(
                                          builder: (_) =>
                                              const SalesReportScreen())))),
                        ],
                      ),
                      const SizedBox(height: 16),
                      _section('الأعلى مبيعاً'),
                      ..._top.map((p) => Card(
                            child: ListTile(
                              leading: const CircleAvatar(
                                  child: Icon(Icons.star)),
                              title: Text(p.productName),
                              subtitle:
                                  Text('${p.quantitySold} قطعة'),
                              trailing: Text(Money.format(p.revenue),
                                  style: const TextStyle(
                                      fontWeight: FontWeight.bold)),
                            ),
                          )),
                    ],
                  ),
                ),
    );
  }

  Widget _section(String t) => Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Text(t,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)));

  Widget _kpiCard(String title, String value, Color color, IconData icon) =>
      Expanded(
        child: Card(
          margin: const EdgeInsets.all(4),
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                      color: color.withOpacity(0.15),
                      shape: BoxShape.circle),
                  child: Icon(icon, color: color),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(title,
                          style: const TextStyle(fontSize: 11, color: Colors.grey)),
                      Text(value,
                          style: const TextStyle(
                              fontSize: 16, fontWeight: FontWeight.bold)),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      );
}
