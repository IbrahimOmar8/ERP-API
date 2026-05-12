import 'package:flutter/material.dart';
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
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('خطأ: $e')));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _loadStock() async {
    if (_selected == null) return;
    setState(() => _loading = true);
    try {
      _items = await InventoryService.getStockByWarehouse(_selected!.id);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('رصيد المخزن')),
      body: Column(
        children: [
          if (_warehouses.isNotEmpty)
            Padding(
              padding: const EdgeInsets.all(12),
              child: DropdownButtonFormField<Warehouse>(
                value: _selected,
                decoration: const InputDecoration(labelText: 'اختر المخزن'),
                items: _warehouses
                    .map((w) => DropdownMenuItem(
                        value: w, child: Text(w.nameAr)))
                    .toList(),
                onChanged: (w) {
                  setState(() => _selected = w);
                  _loadStock();
                },
              ),
            ),
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator())
                : _items.isEmpty
                    ? const Center(child: Text('لا توجد أرصدة'))
                    : ListView.separated(
                        itemCount: _items.length,
                        separatorBuilder: (_, __) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final s = _items[i];
                          return ListTile(
                            title: Text(s.productName),
                            subtitle: Text('SKU: ${s.sku}'),
                            trailing: Column(
                              crossAxisAlignment: CrossAxisAlignment.end,
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Text('${s.quantity}',
                                    style: const TextStyle(
                                        fontSize: 16,
                                        fontWeight: FontWeight.bold)),
                                Text('متاح: ${s.availableQuantity}',
                                    style: const TextStyle(
                                        fontSize: 11, color: Colors.grey)),
                              ],
                            ),
                          );
                        },
                      ),
          ),
        ],
      ),
    );
  }
}
