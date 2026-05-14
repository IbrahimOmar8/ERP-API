import 'package:flutter/material.dart';
import 'package:flutter_barcode_scanner/flutter_barcode_scanner.dart';
import 'package:provider/provider.dart';
import '../../../core/providers/auth_provider.dart';
import '../../../core/providers/cart_provider.dart';
import '../../../core/providers/session_provider.dart';
import '../../../core/utils/money.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/loading_shimmer.dart';
import '../../inventory/models/product.dart';
import '../../inventory/services/inventory_service.dart';
import 'checkout_screen.dart';
import 'session_screen.dart';

class PosScreen extends StatefulWidget {
  const PosScreen({super.key});

  @override
  State<PosScreen> createState() => _PosScreenState();
}

class _PosScreenState extends State<PosScreen> {
  final _searchController = TextEditingController();
  List<Product> _products = [];
  bool _loading = false;

  @override
  void initState() {
    super.initState();
    _ensureSession();
    _search();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _ensureSession() async {
    final auth = context.read<AuthProvider>();
    await context.read<SessionProvider>().loadCurrent(auth.userId);
    if (!mounted) return;
    final session = context.read<SessionProvider>();
    if (!session.isOpen) {
      Navigator.pushReplacement(
          context, MaterialPageRoute(builder: (_) => const SessionScreen()));
    }
  }

  Future<void> _search() async {
    setState(() => _loading = true);
    try {
      _products =
          await InventoryService.getProducts(search: _searchController.text.trim());
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _scan() async {
    try {
      final code = await FlutterBarcodeScanner.scanBarcode(
          '#1976D2', 'إلغاء', true, ScanMode.BARCODE);
      if (code == '-1') return;
      final p = await InventoryService.getByBarcode(code);
      if (p == null) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('لم يتم العثور على الصنف')));
        }
        return;
      }
      if (mounted) context.read<CartProvider>().addProduct(p);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    }
  }

  void _openCartSheet() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Theme.of(context).scaffoldBackgroundColor,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => DraggableScrollableSheet(
        initialChildSize: 0.85,
        minChildSize: 0.5,
        maxChildSize: 0.95,
        expand: false,
        builder: (_, scroll) => _CartView(scrollController: scroll, onCheckout: _goCheckout),
      ),
    );
  }

  void _goCheckout() {
    Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => const CheckoutScreen()),
    );
  }

  @override
  Widget build(BuildContext context) {
    final cart = context.watch<CartProvider>();
    return Scaffold(
      appBar: AppBar(
        title: const Text('نقطة البيع'),
        actions: [
          IconButton(
              icon: const Icon(Icons.qr_code_scanner),
              tooltip: 'مسح باركود',
              onPressed: _scan),
          IconButton(
              icon: const Icon(Icons.refresh),
              tooltip: 'تحديث',
              onPressed: _search),
        ],
      ),
      body: LayoutBuilder(
        builder: (context, constraints) {
          final wide = constraints.maxWidth > 720;
          return wide
              ? Row(
                  children: [
                    Expanded(flex: 3, child: _productsList()),
                    const VerticalDivider(width: 1),
                    Expanded(
                      flex: 2,
                      child: _CartView(onCheckout: _goCheckout),
                    ),
                  ],
                )
              : _productsList();
        },
      ),
      floatingActionButton: LayoutBuilder(
        builder: (context, constraints) {
          if (MediaQuery.of(context).size.width > 720) return const SizedBox.shrink();
          return cart.itemCount == 0
              ? const SizedBox.shrink()
              : FloatingActionButton.extended(
                  onPressed: _openCartSheet,
                  icon: Badge(
                    label: Text('${cart.itemCount}'),
                    child: const Icon(Icons.shopping_cart_checkout),
                  ),
                  label: Text('السلة · ${Money.format(cart.total)}'),
                );
        },
      ),
    );
  }

  Widget _productsList() {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 8, 12, 4),
          child: TextField(
            controller: _searchController,
            decoration: InputDecoration(
              hintText: 'ابحث بالاسم أو SKU...',
              prefixIcon: const Icon(Icons.search),
              suffixIcon: _searchController.text.isEmpty
                  ? null
                  : IconButton(
                      icon: const Icon(Icons.close),
                      onPressed: () {
                        _searchController.clear();
                        _search();
                      },
                    ),
            ),
            onChanged: (_) => setState(() {}),
            onSubmitted: (_) => _search(),
          ),
        ),
        Expanded(
          child: _loading
              ? const LoadingShimmerList(itemCount: 9, itemHeight: 110)
              : _products.isEmpty
                  ? EmptyState(
                      icon: Icons.inventory_2_outlined,
                      message: _searchController.text.isEmpty
                          ? 'لا توجد أصناف'
                          : 'لا توجد نتائج لـ "${_searchController.text}"',
                    )
                  : RefreshIndicator(
                      onRefresh: _search,
                      child: GridView.builder(
                        padding: const EdgeInsets.all(8),
                        gridDelegate:
                            const SliverGridDelegateWithMaxCrossAxisExtent(
                          maxCrossAxisExtent: 160,
                          childAspectRatio: 0.88,
                          crossAxisSpacing: 8,
                          mainAxisSpacing: 8,
                        ),
                        itemCount: _products.length,
                        itemBuilder: (_, i) => _ProductTile(product: _products[i]),
                      ),
                    ),
        ),
      ],
    );
  }
}

class _ProductTile extends StatelessWidget {
  final Product product;
  const _ProductTile({required this.product});

  @override
  Widget build(BuildContext context) {
    final primary = Theme.of(context).colorScheme.primary;
    return InkWell(
      onTap: () => context.read<CartProvider>().addProduct(product),
      borderRadius: BorderRadius.circular(14),
      child: Container(
        decoration: BoxDecoration(
          color: Theme.of(context).cardTheme.color ?? Colors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: primary.withOpacity(0.12)),
        ),
        padding: const EdgeInsets.all(10),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: primary.withOpacity(0.08),
                borderRadius: BorderRadius.circular(10),
              ),
              child: Icon(Icons.inventory_2, color: primary, size: 28),
            ),
            const SizedBox(height: 8),
            Text(
              product.nameAr,
              maxLines: 2,
              textAlign: TextAlign.center,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 12.5),
            ),
            const Spacer(),
            Text(
              Money.format(product.salePrice),
              style: const TextStyle(fontWeight: FontWeight.bold, color: Colors.green, fontSize: 14),
            ),
            Text('متاح: ${product.currentStock}',
                style: const TextStyle(fontSize: 10.5, color: Colors.grey)),
          ],
        ),
      ),
    );
  }
}

class _CartView extends StatelessWidget {
  final ScrollController? scrollController;
  final VoidCallback onCheckout;
  const _CartView({this.scrollController, required this.onCheckout});

  @override
  Widget build(BuildContext context) {
    final cart = context.watch<CartProvider>();
    final primary = Theme.of(context).colorScheme.primary;

    return Column(
      children: [
        Container(
          width: double.infinity,
          decoration: BoxDecoration(
            color: primary.withOpacity(0.08),
            borderRadius: scrollController != null
                ? const BorderRadius.vertical(top: Radius.circular(20))
                : null,
          ),
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              if (scrollController != null) ...[
                Container(
                  width: 40,
                  height: 4,
                  decoration: BoxDecoration(
                    color: Colors.grey.shade400,
                    borderRadius: BorderRadius.circular(4),
                  ),
                ),
                const SizedBox(width: 8),
              ],
              Icon(Icons.shopping_cart, color: primary, size: 20),
              const SizedBox(width: 8),
              Expanded(
                child: Text('سلة المشتريات (${cart.itemCount})',
                    style: TextStyle(
                        fontWeight: FontWeight.bold, color: primary)),
              ),
              IconButton(
                icon: const Icon(Icons.delete_sweep, color: Colors.red),
                tooltip: 'إفراغ السلة',
                onPressed: cart.itemCount == 0 ? null : () => cart.clear(),
              ),
            ],
          ),
        ),
        Expanded(
          child: cart.itemCount == 0
              ? const EmptyState(
                  icon: Icons.shopping_cart_outlined,
                  message: 'السلة فارغة\nاضغط على أي صنف لإضافته',
                )
              : ListView.builder(
                  controller: scrollController,
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
                  itemCount: cart.lines.length,
                  itemBuilder: (_, i) {
                    final line = cart.lines[i];
                    return Padding(
                      padding: const EdgeInsets.only(bottom: 6),
                      child: Card(
                        child: Padding(
                          padding: const EdgeInsets.fromLTRB(12, 8, 8, 8),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Expanded(
                                    child: Text(line.product.nameAr,
                                        style: const TextStyle(
                                            fontWeight: FontWeight.bold)),
                                  ),
                                  IconButton(
                                    icon: const Icon(Icons.close, size: 18),
                                    onPressed: () => cart.removeAt(i),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 4),
                              Row(
                                children: [
                                  _QtyButton(
                                    icon: Icons.remove,
                                    onTap: () => cart.updateQuantity(i, line.quantity - 1),
                                  ),
                                  Container(
                                    margin: const EdgeInsets.symmetric(horizontal: 8),
                                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
                                    decoration: BoxDecoration(
                                      color: Theme.of(context).colorScheme.primary.withOpacity(0.08),
                                      borderRadius: BorderRadius.circular(8),
                                    ),
                                    child: Text('${line.quantity}',
                                        style: const TextStyle(
                                            fontSize: 15, fontWeight: FontWeight.bold)),
                                  ),
                                  _QtyButton(
                                    icon: Icons.add,
                                    onTap: () => cart.updateQuantity(i, line.quantity + 1),
                                  ),
                                  const Spacer(),
                                  Text(Money.format(line.lineTotal),
                                      style: const TextStyle(
                                          fontWeight: FontWeight.bold, fontSize: 15)),
                                ],
                              ),
                            ],
                          ),
                        ),
                      ),
                    );
                  },
                ),
        ),
        Container(
          decoration: BoxDecoration(
            color: Theme.of(context).cardTheme.color ?? Colors.white,
            border: Border(
              top: BorderSide(color: Theme.of(context).dividerTheme.color ?? Colors.grey.shade200),
            ),
          ),
          padding: const EdgeInsets.all(12),
          child: Column(
            children: [
              _row(context, 'المجموع', Money.format(cart.subTotal)),
              _row(context, 'الخصم',
                  Money.format(cart.lineDiscounts + cart.invoiceDiscount),
                  color: Colors.red),
              _row(context, 'الضريبة', Money.format(cart.vat)),
              const Divider(),
              _row(context, 'الإجمالي', Money.format(cart.total),
                  bold: true, size: 20),
              const SizedBox(height: 10),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  onPressed: cart.itemCount == 0 ? null : onCheckout,
                  icon: const Icon(Icons.payment),
                  label: const Text('متابعة للدفع'),
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 14),
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _row(BuildContext context, String label, String value,
      {bool bold = false, double size = 14, Color? color}) {
    final style = TextStyle(
      fontSize: size,
      fontWeight: bold ? FontWeight.bold : FontWeight.normal,
      color: color,
    );
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [Text(label, style: style), Text(value, style: style)],
      ),
    );
  }
}

class _QtyButton extends StatelessWidget {
  final IconData icon;
  final VoidCallback onTap;
  const _QtyButton({required this.icon, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(8),
      child: Container(
        width: 32,
        height: 32,
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.primary.withOpacity(0.08),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Icon(icon, size: 18, color: Theme.of(context).colorScheme.primary),
      ),
    );
  }
}
