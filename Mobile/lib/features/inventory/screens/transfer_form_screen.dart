import 'package:flutter/material.dart';
import '../models/product.dart';
import '../models/warehouse.dart';
import '../services/inventory_service.dart';

class TransferFormScreen extends StatefulWidget {
  const TransferFormScreen({super.key});

  @override
  State<TransferFormScreen> createState() => _TransferFormScreenState();
}

class _TransferFormScreenState extends State<TransferFormScreen> {
  List<Warehouse> _warehouses = [];
  List<Product> _products = [];
  Warehouse? _from;
  Warehouse? _to;
  final List<_Line> _lines = [];
  final _notesCtrl = TextEditingController();
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final wh = await InventoryService.getWarehouses();
    final ps = await InventoryService.getProducts();
    if (!mounted) return;
    setState(() {
      _warehouses = wh.where((w) => w.isActive).toList();
      _products = ps;
      if (_warehouses.length >= 2) {
        _from = _warehouses[0];
        _to = _warehouses[1];
      }
    });
  }

  Future<void> _addLine() async {
    if (_products.isEmpty) return;
    final p = await showModalBottomSheet<Product>(
      context: context,
      isScrollControlled: true,
      builder: (ctx) {
        var search = '';
        return StatefulBuilder(builder: (ctx, set) {
          final filtered = _products
              .where((p) =>
                  search.isEmpty ||
                  p.nameAr.contains(search) ||
                  p.sku.toLowerCase().contains(search.toLowerCase()))
              .toList();
          return SafeArea(
            child: Padding(
              padding: EdgeInsets.only(
                  bottom: MediaQuery.of(ctx).viewInsets.bottom),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Padding(
                    padding: const EdgeInsets.all(12),
                    child: TextField(
                      autofocus: true,
                      decoration: const InputDecoration(
                          prefixIcon: Icon(Icons.search),
                          hintText: 'ابحث عن صنف'),
                      onChanged: (v) => set(() => search = v),
                    ),
                  ),
                  SizedBox(
                    height: MediaQuery.of(ctx).size.height * 0.5,
                    child: ListView.builder(
                      itemCount: filtered.length,
                      itemBuilder: (_, i) => ListTile(
                        title: Text(filtered[i].nameAr),
                        subtitle: Text(filtered[i].sku),
                        onTap: () => Navigator.pop(ctx, filtered[i]),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          );
        });
      },
    );
    if (p != null) {
      setState(() => _lines.add(_Line(product: p, quantity: 1)));
    }
  }

  Future<void> _save() async {
    if (_from == null || _to == null) return;
    if (_from!.id == _to!.id) {
      ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('لا يمكن التحويل لنفس المخزن')));
      return;
    }
    if (_lines.isEmpty) return;
    setState(() => _saving = true);
    try {
      await InventoryService.createTransfer(
        fromWarehouseId: _from!.id,
        toWarehouseId: _to!.id,
        notes: _notesCtrl.text.trim().isEmpty ? null : _notesCtrl.text.trim(),
        items: _lines
            .map((l) => {
                  'productId': l.product.id,
                  'quantity': l.quantity,
                })
            .toList(),
      );
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('تحويل جديد'),
        actions: [
          IconButton(
            icon: const Icon(Icons.save),
            onPressed: _saving ? null : _save,
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(12),
        children: [
          DropdownButtonFormField<Warehouse>(
            value: _from,
            decoration: const InputDecoration(labelText: 'من مخزن'),
            items: _warehouses
                .map((w) =>
                    DropdownMenuItem(value: w, child: Text(w.nameAr)))
                .toList(),
            onChanged: (v) => setState(() => _from = v),
          ),
          const SizedBox(height: 8),
          DropdownButtonFormField<Warehouse>(
            value: _to,
            decoration: const InputDecoration(labelText: 'إلى مخزن'),
            items: _warehouses
                .map((w) =>
                    DropdownMenuItem(value: w, child: Text(w.nameAr)))
                .toList(),
            onChanged: (v) => setState(() => _to = v),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: _notesCtrl,
            decoration: const InputDecoration(labelText: 'ملاحظات'),
          ),
          const SizedBox(height: 16),
          const Text('الأصناف', style: TextStyle(fontWeight: FontWeight.bold)),
          ..._lines.asMap().entries.map((e) => Card(
                child: ListTile(
                  title: Text(e.value.product.nameAr),
                  subtitle: Text(e.value.product.sku),
                  trailing: SizedBox(
                    width: 100,
                    child: TextFormField(
                      initialValue: e.value.quantity.toString(),
                      keyboardType: TextInputType.number,
                      textAlign: TextAlign.center,
                      onChanged: (v) =>
                          e.value.quantity = double.tryParse(v) ?? 0,
                      decoration: const InputDecoration(labelText: 'الكمية'),
                    ),
                  ),
                  leading: IconButton(
                    icon: const Icon(Icons.delete, color: Colors.red),
                    onPressed: () => setState(() => _lines.removeAt(e.key)),
                  ),
                ),
              )),
          const SizedBox(height: 8),
          OutlinedButton.icon(
            icon: const Icon(Icons.add),
            label: const Text('إضافة صنف'),
            onPressed: _addLine,
          ),
        ],
      ),
    );
  }
}

class _Line {
  final Product product;
  double quantity;
  _Line({required this.product, required this.quantity});
}
