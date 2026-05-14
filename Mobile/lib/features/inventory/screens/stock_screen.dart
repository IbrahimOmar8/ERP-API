import 'package:flutter/material.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/loading_shimmer.dart';
import '../models/stock_item.dart';
import '../models/warehouse.dart';
import '../services/inventory_service.dart';

class StockScreen extends StatefulWidget {
  const StockScreen({super.key});

  @override
  State<StockScreen> createState() => _StockScreenState();
}

class _StockScreenState extends State<StockScreen> {
  List<Warehouse> _warehouses = [];
  List<StockItem> _items = [];
  Warehouse? _selected;
  bool _loading = true;
  String _search = '';

  @override
  void initState() {
    super.initState();
    _loadWarehouses();
  }

  Future<void> _loadWarehouses() async {
    try {
      _warehouses = await InventoryService.getWarehouses();
      if (_warehouses.isNotEmpty) {
        _selected = _warehouses.firstWhere((w) => w.isMain,
            orElse: () => _warehouses.first);
        await _loadStock();
      } else {
        if (mounted) setState(() => _loading = false);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('خطأ: $e')));
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _loadStock() async {
    if (_selected == null) return;
    setState(() => _loading = true);
    try {
      _items = await InventoryService.getStockByWarehouse(_selected!.id);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('خطأ: $e')));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final filtered = _search.isEmpty
        ? _items
        : _items
            .where((s) =>
                s.productName.contains(_search) ||
                s.sku.toLowerCase().contains(_search.toLowerCase()))
            .toList();

    return Scaffold(
      appBar: AppBar(
        title: const Text('رصيد المخزن'),
        actions: [
          if (_selected != null)
            IconButton(icon: const Icon(Icons.refresh), onPressed: _loadStock),
        ],
      ),
      body: Column(
        children: [
          if (_warehouses.isNotEmpty)
            Padding(
              padding: const EdgeInsets.fromLTRB(12, 12, 12, 6),
              child: DropdownButtonFormField<Warehouse>(
                value: _selected,
                decoration: const InputDecoration(
                  labelText: 'المخزن',
                  prefixIcon: Icon(Icons.warehouse),
                ),
                items: _warehouses
                    .map((w) =>
                        DropdownMenuItem(value: w, child: Text(w.nameAr)))
                    .toList(),
                onChanged: (w) {
                  setState(() => _selected = w);
                  _loadStock();
                },
              ),
            ),
          if (_warehouses.isNotEmpty)
            Padding(
              padding: const EdgeInsets.fromLTRB(12, 6, 12, 6),
              child: TextField(
                decoration: const InputDecoration(
                  prefixIcon: Icon(Icons.search),
                  hintText: 'ابحث عن صنف...',
                ),
                onChanged: (v) => setState(() => _search = v),
              ),
            ),
          Expanded(
            child: _loading
                ? const LoadingShimmerList(itemHeight: 70)
                : _warehouses.isEmpty
                    ? const EmptyState(
                        icon: Icons.warehouse_outlined,
                        message: 'لا توجد مخازن',
                      )
                    : filtered.isEmpty
                        ? EmptyState(
                            icon: Icons.inventory_outlined,
                            message: _items.isEmpty
                                ? 'لا توجد أرصدة في هذا المخزن'
                                : 'لا توجد نتائج',
                          )
                        : RefreshIndicator(
                            onRefresh: _loadStock,
                            child: ListView.separated(
                              padding: const EdgeInsets.fromLTRB(12, 0, 12, 24),
                              itemCount: filtered.length,
                              separatorBuilder: (_, __) =>
                                  const SizedBox(height: 6),
                              itemBuilder: (_, i) {
                                final s = filtered[i];
                                final low = s.quantity <= 0;
                                return Card(
                                  child: ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor:
                                          (low ? Colors.red : Colors.teal)
                                              .withOpacity(0.12),
                                      child: Icon(
                                        low
                                            ? Icons.warning_amber
                                            : Icons.check_circle_outline,
                                        color: low ? Colors.red : Colors.teal,
                                      ),
                                    ),
                                    title: Text(s.productName,
                                        style: const TextStyle(
                                            fontWeight: FontWeight.w600)),
                                    subtitle: Text('SKU: ${s.sku}',
                                        style: const TextStyle(fontSize: 12)),
                                    trailing: Column(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.end,
                                      mainAxisAlignment:
                                          MainAxisAlignment.center,
                                      children: [
                                        Text('${s.quantity}',
                                            style: TextStyle(
                                                fontSize: 17,
                                                fontWeight: FontWeight.bold,
                                                color: low
                                                    ? Colors.red
                                                    : null)),
                                        Text('متاح: ${s.availableQuantity}',
                                            style: const TextStyle(
                                                fontSize: 11,
                                                color: Colors.grey)),
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
}
