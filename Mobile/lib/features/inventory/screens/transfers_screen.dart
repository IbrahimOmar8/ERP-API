import 'package:flutter/material.dart';
import '../../../core/utils/money.dart';
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
    return Scaffold(
      appBar: AppBar(
        title: const Text('تحويلات المخازن'),
        actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: _load)],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          final created = await Navigator.push<bool>(
              context,
              MaterialPageRoute(
                  builder: (_) => const TransferFormScreen()));
          if (created == true) _load();
        },
        icon: const Icon(Icons.add),
        label: const Text('تحويل جديد'),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _items.isEmpty
              ? const Center(child: Text('لا توجد تحويلات'))
              : ListView.separated(
                  itemCount: _items.length,
                  separatorBuilder: (_, __) => const Divider(height: 1),
                  itemBuilder: (_, i) {
                    final t = _items[i];
                    return ListTile(
                      leading: CircleAvatar(
                        backgroundColor:
                            (t.isCompleted ? Colors.green : Colors.orange)
                                .withOpacity(0.15),
                        child: Icon(
                          t.isCompleted ? Icons.check : Icons.swap_horiz,
                          color: t.isCompleted ? Colors.green : Colors.orange,
                        ),
                      ),
                      title: Text(t.transferNumber),
                      subtitle: Text(
                          '${t.fromWarehouseName} ← ${t.toWarehouseName}\n'
                          '${t.items.length} صنف • قيمة ${Money.format(t.totalValue)}'),
                      isThreeLine: true,
                      trailing: t.isCompleted
                          ? null
                          : PopupMenuButton<String>(
                              onSelected: (v) {
                                if (v == 'complete') _complete(t);
                                if (v == 'cancel') _cancel(t);
                              },
                              itemBuilder: (_) => const [
                                PopupMenuItem(
                                    value: 'complete',
                                    child: Text('تنفيذ التحويل')),
                                PopupMenuItem(
                                    value: 'cancel', child: Text('إلغاء')),
                              ],
                            ),
                    );
                  },
                ),
    );
  }
}
