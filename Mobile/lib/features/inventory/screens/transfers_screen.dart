import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../core/utils/money.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/loading_shimmer.dart';
import '../models/stock_transfer.dart';
import '../services/inventory_service.dart';
import 'transfer_form_screen.dart';

class TransfersScreen extends StatefulWidget {
  const TransfersScreen({super.key});

  @override
  State<TransfersScreen> createState() => _TransfersScreenState();
}

class _TransfersScreenState extends State<TransfersScreen> {
  List<StockTransfer> _items = [];
  bool _loading = true;
  bool _showCompleted = true;

  static final _df = DateFormat('yyyy-MM-dd HH:mm');

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      _items = await InventoryService.getTransfers();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _complete(StockTransfer t) async {
    try {
      await InventoryService.completeTransfer(t.id);
      await _load();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('تم تنفيذ التحويل')));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$e')));
      }
    }
  }

  Future<void> _cancel(StockTransfer t) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('إلغاء التحويل'),
        content: Text('هل أنت متأكد من إلغاء التحويل ${t.transferNumber}؟'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('تراجع')),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('تأكيد'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    try {
      await InventoryService.cancelTransfer(t.id);
      await _load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$e')));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final filtered = _showCompleted
        ? _items
        : _items.where((t) => !t.isCompleted).toList();
    final pending = _items.where((t) => !t.isCompleted).length;
    final completed = _items.length - pending;

    return Scaffold(
      appBar: AppBar(
        title: const Text('تحويلات المخازن'),
        actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: _load)],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          final created = await Navigator.push<bool>(
              context,
              MaterialPageRoute(builder: (_) => const TransferFormScreen()));
          if (created == true) _load();
        },
        icon: const Icon(Icons.add),
        label: const Text('تحويل جديد'),
      ),
      body: Column(
        children: [
          if (_items.isNotEmpty)
            Padding(
              padding: const EdgeInsets.fromLTRB(12, 12, 12, 0),
              child: Row(
                children: [
                  Expanded(
                    child: _summaryTile(
                      icon: Icons.pending,
                      color: Colors.orange,
                      label: 'قيد التنفيذ',
                      value: pending.toString(),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _summaryTile(
                      icon: Icons.check_circle,
                      color: Colors.green,
                      label: 'منفذة',
                      value: completed.toString(),
                    ),
                  ),
                ],
              ),
            ),
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 8, 12, 4),
            child: Row(
              children: [
                const Text('عرض المكتملة'),
                Switch(
                  value: _showCompleted,
                  onChanged: (v) => setState(() => _showCompleted = v),
                ),
                const Spacer(),
                Text('${filtered.length} تحويل',
                    style: const TextStyle(color: Colors.grey, fontSize: 12)),
              ],
            ),
          ),
          Expanded(
            child: _loading
                ? const LoadingShimmerList(itemHeight: 100)
                : filtered.isEmpty
                    ? EmptyState(
                        icon: Icons.swap_horiz_outlined,
                        message: _items.isEmpty
                            ? 'لا توجد تحويلات بعد'
                            : 'لا توجد تحويلات بهذا الفلتر',
                      )
                    : RefreshIndicator(
                        onRefresh: _load,
                        child: ListView.separated(
                          padding: const EdgeInsets.fromLTRB(12, 0, 12, 80),
                          itemCount: filtered.length,
                          separatorBuilder: (_, __) =>
                              const SizedBox(height: 6),
                          itemBuilder: (_, i) {
                            final t = filtered[i];
                            final color =
                                t.isCompleted ? Colors.green : Colors.orange;
                            return Card(
                              child: Padding(
                                padding: const EdgeInsets.all(12),
                                child: Column(
                                  crossAxisAlignment:
                                      CrossAxisAlignment.start,
                                  children: [
                                    Row(
                                      children: [
                                        Container(
                                          padding: const EdgeInsets.all(8),
                                          decoration: BoxDecoration(
                                            color: color.withOpacity(0.15),
                                            borderRadius:
                                                BorderRadius.circular(10),
                                          ),
                                          child: Icon(
                                            t.isCompleted
                                                ? Icons.check
                                                : Icons.swap_horiz,
                                            color: color,
                                            size: 20,
                                          ),
                                        ),
                                        const SizedBox(width: 10),
                                        Expanded(
                                          child: Column(
                                            crossAxisAlignment:
                                                CrossAxisAlignment.start,
                                            children: [
                                              Text(t.transferNumber,
                                                  style: const TextStyle(
                                                      fontWeight:
                                                          FontWeight.bold,
                                                      fontSize: 15)),
                                              Text(
                                                _df.format(t.transferDate.toLocal()),
                                                style: const TextStyle(
                                                    color: Colors.grey,
                                                    fontSize: 11),
                                              ),
                                            ],
                                          ),
                                        ),
                                        Container(
                                          padding: const EdgeInsets.symmetric(
                                              horizontal: 8, vertical: 3),
                                          decoration: BoxDecoration(
                                            color: color.withOpacity(0.12),
                                            borderRadius:
                                                BorderRadius.circular(20),
                                          ),
                                          child: Text(
                                            t.isCompleted ? 'منفذ' : 'معلّق',
                                            style: TextStyle(
                                                color: color,
                                                fontSize: 11,
                                                fontWeight: FontWeight.bold),
                                          ),
                                        ),
                                      ],
                                    ),
                                    const Divider(height: 16),
                                    Row(
                                      children: [
                                        Expanded(
                                          child: Column(
                                            crossAxisAlignment:
                                                CrossAxisAlignment.start,
                                            children: [
                                              const Text('من',
                                                  style: TextStyle(
                                                      fontSize: 10,
                                                      color: Colors.grey)),
                                              Text(t.fromWarehouseName,
                                                  style: const TextStyle(
                                                      fontWeight:
                                                          FontWeight.w600)),
                                            ],
                                          ),
                                        ),
                                        const Icon(Icons.arrow_back, size: 18),
                                        Expanded(
                                          child: Column(
                                            crossAxisAlignment:
                                                CrossAxisAlignment.start,
                                            children: [
                                              const Text('إلى',
                                                  style: TextStyle(
                                                      fontSize: 10,
                                                      color: Colors.grey)),
                                              Text(t.toWarehouseName,
                                                  style: const TextStyle(
                                                      fontWeight:
                                                          FontWeight.w600)),
                                            ],
                                          ),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 8),
                                    Row(
                                      children: [
                                        const Icon(Icons.inventory_2,
                                            size: 14, color: Colors.grey),
                                        const SizedBox(width: 4),
                                        Text('${t.items.length} صنف',
                                            style: const TextStyle(
                                                fontSize: 12,
                                                color: Colors.grey)),
                                        const SizedBox(width: 12),
                                        const Icon(Icons.attach_money,
                                            size: 14, color: Colors.grey),
                                        Text(Money.format(t.totalValue),
                                            style: const TextStyle(
                                                fontSize: 12,
                                                fontWeight: FontWeight.bold)),
                                        const Spacer(),
                                        if (!t.isCompleted)
                                          PopupMenuButton<String>(
                                            icon: const Icon(Icons.more_vert),
                                            onSelected: (v) {
                                              if (v == 'complete') _complete(t);
                                              if (v == 'cancel') _cancel(t);
                                            },
                                            itemBuilder: (_) => const [
                                              PopupMenuItem(
                                                value: 'complete',
                                                child: Row(
                                                  children: [
                                                    Icon(Icons.check,
                                                        color: Colors.green,
                                                        size: 18),
                                                    SizedBox(width: 8),
                                                    Text('تنفيذ التحويل'),
                                                  ],
                                                ),
                                              ),
                                              PopupMenuItem(
                                                value: 'cancel',
                                                child: Row(
                                                  children: [
                                                    Icon(Icons.cancel,
                                                        color: Colors.red,
                                                        size: 18),
                                                    SizedBox(width: 8),
                                                    Text('إلغاء التحويل'),
                                                  ],
                                                ),
                                              ),
                                            ],
                                          ),
                                      ],
                                    ),
                                  ],
                                ),
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

  Widget _summaryTile({
    required IconData icon,
    required Color color,
    required String label,
    required String value,
  }) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: color.withOpacity(0.08),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withOpacity(0.18)),
      ),
      child: Row(
        children: [
          Icon(icon, color: color, size: 20),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(label,
                    style: const TextStyle(fontSize: 11, color: Colors.grey)),
                Text(value,
                    style: const TextStyle(
                        fontSize: 17, fontWeight: FontWeight.bold)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
