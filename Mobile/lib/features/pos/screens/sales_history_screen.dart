import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../../../core/providers/auth_provider.dart';
import '../../../core/utils/money.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/loading_shimmer.dart';
import '../models/sale.dart';
import '../services/pos_service.dart';
import 'receipt_screen.dart';

class SalesHistoryScreen extends StatefulWidget {
  const SalesHistoryScreen({super.key});

  @override
  State<SalesHistoryScreen> createState() => _SalesHistoryScreenState();
}

class _SalesHistoryScreenState extends State<SalesHistoryScreen> {
  List<Sale> _sales = [];
  bool _loading = true;
  String _search = '';

  static final _df = DateFormat('yyyy-MM-dd HH:mm');

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final auth = context.read<AuthProvider>();
      _sales = await PosService.getSales(cashierId: auth.userId);
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
    final filtered = _search.isEmpty
        ? _sales
        : _sales
            .where((s) =>
                s.invoiceNumber.contains(_search) ||
                (s.customerName ?? '').contains(_search))
            .toList();

    final today = DateTime.now();
    bool isToday(DateTime d) =>
        d.year == today.year && d.month == today.month && d.day == today.day;
    final todayTotal = _sales
        .where((s) => isToday(s.saleDate) && s.status == 1)
        .fold<double>(0, (sum, s) => sum + s.total);
    final todayCount =
        _sales.where((s) => isToday(s.saleDate) && s.status == 1).length;

    return Scaffold(
      appBar: AppBar(
        title: const Text('سجل الفواتير'),
        actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: _load)],
      ),
      body: Column(
        children: [
          if (_sales.isNotEmpty)
            Container(
              margin: const EdgeInsets.all(12),
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  colors: [
                    Theme.of(context).colorScheme.primary,
                    Theme.of(context).colorScheme.primary.withOpacity(0.7),
                  ],
                ),
                borderRadius: BorderRadius.circular(14),
              ),
              child: Row(
                children: [
                  const Icon(Icons.today, color: Colors.white, size: 22),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('فواتير اليوم',
                            style: TextStyle(color: Colors.white70, fontSize: 12)),
                        Text(
                          '$todayCount فاتورة · ${Money.format(todayTotal)}',
                          style: const TextStyle(
                              color: Colors.white,
                              fontSize: 15,
                              fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: TextField(
              decoration: const InputDecoration(
                prefixIcon: Icon(Icons.search),
                hintText: 'ابحث برقم الفاتورة أو العميل...',
              ),
              onChanged: (v) => setState(() => _search = v),
            ),
          ),
          const SizedBox(height: 8),
          Expanded(
            child: _loading
                ? const LoadingShimmerList(itemHeight: 86)
                : filtered.isEmpty
                    ? EmptyState(
                        icon: Icons.receipt_long_outlined,
                        message: _sales.isEmpty
                            ? 'لم تسجل أي فاتورة بعد'
                            : 'لا توجد نتائج',
                      )
                    : RefreshIndicator(
                        onRefresh: _load,
                        child: ListView.separated(
                          padding: const EdgeInsets.fromLTRB(12, 0, 12, 24),
                          itemCount: filtered.length,
                          separatorBuilder: (_, __) => const SizedBox(height: 6),
                          itemBuilder: (_, i) {
                            final s = filtered[i];
                            return Card(
                              child: ListTile(
                                leading: CircleAvatar(
                                  backgroundColor:
                                      _statusColor(s.status).withOpacity(0.15),
                                  child: Icon(_statusIcon(s.status),
                                      color: _statusColor(s.status), size: 20),
                                ),
                                title: Row(
                                  children: [
                                    Expanded(
                                      child: Text(s.invoiceNumber,
                                          style: const TextStyle(
                                              fontWeight: FontWeight.w700)),
                                    ),
                                    if (s.eInvoiceUuid != null)
                                      const _StatusChip(
                                          label: 'ETA ✓', color: Colors.green),
                                  ],
                                ),
                                subtitle: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(_df.format(s.saleDate.toLocal()),
                                        style: const TextStyle(fontSize: 12)),
                                    Text(s.customerName ?? 'عميل نقدي',
                                        style: const TextStyle(
                                            fontSize: 12, color: Colors.grey)),
                                  ],
                                ),
                                trailing: Text(
                                  Money.format(s.total),
                                  style: const TextStyle(
                                      fontWeight: FontWeight.bold, fontSize: 15),
                                ),
                                onTap: () => Navigator.push(
                                    context,
                                    MaterialPageRoute(
                                        builder: (_) => ReceiptScreen(sale: s))),
                              ),
                            );
                          },
                        ),
                      ),
          ),
        ],
      ),
    );
  }

  Color _statusColor(int status) {
    switch (status) {
      case 1: return Colors.green;
      case 2: return Colors.grey;
      case 3: return Colors.red;
      case 4: return Colors.orange;
      default: return Colors.grey;
    }
  }

  IconData _statusIcon(int status) {
    switch (status) {
      case 1: return Icons.check_circle;
      case 2: return Icons.cancel;
      case 3: return Icons.undo;
      case 4: return Icons.swap_horiz;
      default: return Icons.receipt;
    }
  }
}

class _StatusChip extends StatelessWidget {
  final String label;
  final Color color;
  const _StatusChip({required this.label, required this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        color: color.withOpacity(0.12),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(label,
          style: TextStyle(
              color: color, fontSize: 11, fontWeight: FontWeight.w600)),
    );
  }
}
