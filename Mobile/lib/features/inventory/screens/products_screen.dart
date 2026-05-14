import 'package:flutter/material.dart';
import '../../../core/utils/money.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/loading_shimmer.dart';
import '../models/product.dart';
import '../services/inventory_service.dart';

class ProductsScreen extends StatefulWidget {
  const ProductsScreen({super.key});

  @override
  State<ProductsScreen> createState() => _ProductsScreenState();
}

class _ProductsScreenState extends State<ProductsScreen> {
  final _searchController = TextEditingController();
  List<Product> _products = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      _products = await InventoryService.getProducts(
          search: _searchController.text.trim());
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
        title: const Text('الأصناف'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _load),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: TextField(
              controller: _searchController,
              decoration: InputDecoration(
                hintText: 'ابحث باسم الصنف أو الباركود',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: _searchController.text.isEmpty
                    ? null
                    : IconButton(
                        icon: const Icon(Icons.close),
                        onPressed: () {
                          _searchController.clear();
                          _load();
                        },
                      ),
              ),
              onChanged: (_) => setState(() {}),
              onSubmitted: (_) => _load(),
            ),
          ),
          Expanded(
            child: _loading
                ? const LoadingShimmerList(itemHeight: 78)
                : _error != null
                    ? EmptyState(
                        icon: Icons.cloud_off,
                        message: 'تعذر التحميل: $_error',
                        actionLabel: 'إعادة المحاولة',
                        onAction: _load,
                      )
                    : _products.isEmpty
                        ? const EmptyState(
                            icon: Icons.inventory_2_outlined,
                            message: 'لا توجد أصناف',
                          )
                        : RefreshIndicator(
                            onRefresh: _load,
                            child: ListView.separated(
                              padding: const EdgeInsets.fromLTRB(12, 0, 12, 24),
                              itemCount: _products.length,
                              separatorBuilder: (_, __) =>
                                  const SizedBox(height: 6),
                              itemBuilder: (context, i) {
                                final p = _products[i];
                                final lowStock =
                                    p.currentStock <= 0 ||
                                        (p.currentStock < 5 && p.trackStock);
                                return Card(
                                  child: ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor: Theme.of(context)
                                          .colorScheme
                                          .primary
                                          .withOpacity(0.12),
                                      child: Icon(Icons.inventory_2,
                                          color: Theme.of(context)
                                              .colorScheme
                                              .primary),
                                    ),
                                    title: Text(p.nameAr,
                                        style: const TextStyle(
                                            fontWeight: FontWeight.w600)),
                                    subtitle: Row(
                                      children: [
                                        Text('SKU: ${p.sku}',
                                            style: const TextStyle(fontSize: 11)),
                                        const Text(' · ',
                                            style: TextStyle(fontSize: 11)),
                                        Icon(
                                          lowStock ? Icons.warning_amber : Icons.check,
                                          size: 12,
                                          color: lowStock ? Colors.orange : Colors.green,
                                        ),
                                        const SizedBox(width: 2),
                                        Text('متاح: ${p.currentStock}',
                                            style: TextStyle(
                                                fontSize: 11,
                                                color: lowStock
                                                    ? Colors.orange
                                                    : Colors.green)),
                                      ],
                                    ),
                                    trailing: Column(
                                      crossAxisAlignment: CrossAxisAlignment.end,
                                      mainAxisAlignment: MainAxisAlignment.center,
                                      children: [
                                        Text(Money.format(p.salePrice),
                                            style: const TextStyle(
                                                fontWeight: FontWeight.bold,
                                                fontSize: 14)),
                                        Text('${p.vatRate.toStringAsFixed(1)}% ضريبة',
                                            style: const TextStyle(
                                                fontSize: 10, color: Colors.grey)),
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
