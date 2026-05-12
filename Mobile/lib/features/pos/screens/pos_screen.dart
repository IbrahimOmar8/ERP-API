import 'package:flutter/material.dart';
import 'package:flutter_barcode_scanner/flutter_barcode_scanner.dart';
import 'package:provider/provider.dart';
import '../../../core/providers/auth_provider.dart';
import '../../../core/providers/cart_provider.dart';
import '../../../core/providers/session_provider.dart';
import '../../../core/utils/money.dart';
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
          '#ff6666', 'إلغاء', true, ScanMode.BARCODE);
      if (code == '-1') return;
      final p = await InventoryService.getByBarcode(code);
      if (p == null) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('لم يتم العثور على الصنف')));
        }
        return;
      }
      context.read<CartProvider>().addProduct(p);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final cart = context.watch<CartProvider>();
    return Scaffold(
      appBar: AppBar(
        title: const Text('نقاط البيع'),
        actions: [
          IconButton(
              icon: const Icon(Icons.qr_code_scanner), onPressed: _scan),
          IconButton(icon: const Icon(Icons.refresh), onPressed: _search),
        ],
      ),
      body: Row(
        children: [
          Expanded(flex: 3, child: _productsList()),
          const VerticalDivider(width: 1),
          Expanded(flex: 2, child: _cartView(cart)),
        ],
      ),
    );
  }

  Widget _productsList() {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(8),
          child: TextField(
            controller: _searchController,
            decoration: InputDecoration(
              hintText: 'ابحث عن الصنف',
              prefixIcon: const Icon(Icons.search),
              suffixIcon: IconButton(
                  icon: const Icon(Icons.send), onPressed: _search),
            ),
            onSubmitted: (_) => _search(),
          ),
        ),
        Expanded(
          child: _loading
              ? const Center(child: CircularProgressIndicator())
              : GridView.builder(
                  padding: const EdgeInsets.all(8),
                  gridDelegate:
                      const SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: 3,
                    childAspectRatio: 0.9,
                    crossAxisSpacing: 8,
                    mainAxisSpacing: 8,
                  ),
                  itemCount: _products.length,
                  itemBuilder: (_, i) {
                    final p = _products[i];
                    return InkWell(
                      onTap: () => context.read<CartProvider>().addProduct(p),
                      child: Card(
                        child: Padding(
                          padding: const EdgeInsets.all(8),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.center,
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Icon(Icons.inventory_2,
                                  size: 36,
                                  color: Theme.of(context).colorScheme.primary),
                              const SizedBox(height: 6),
                              Text(p.nameAr,
                                  maxLines: 2,
                                  textAlign: TextAlign.center,
                                  overflow: TextOverflow.ellipsis,
                                  style: const TextStyle(
                                      fontWeight: FontWeight.w600,
                                      fontSize: 13)),
                              const Spacer(),
                              Text(Money.format(p.salePrice),
                                  style: const TextStyle(
                                      fontWeight: FontWeight.bold,
                                      color: Colors.green)),
                              Text('متاح: ${p.currentStock}',
                                  style: const TextStyle(
                                      fontSize: 11, color: Colors.grey)),
                            ],
                          ),
                        ),
                      ),
                    );
                  },
                ),
        ),
      ],
    );
  }

  Widget _cartView(CartProvider cart) {
    return Column(
      children: [
        Container(
          width: double.infinity,
          color: Theme.of(context).colorScheme.primaryContainer,
          padding: const EdgeInsets.all(12),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('سلة المشتريات (${cart.itemCount})',
                  style: const TextStyle(fontWeight: FontWeight.bold)),
              IconButton(
                icon: const Icon(Icons.delete_sweep, color: Colors.red),
                onPressed: cart.itemCount == 0 ? null : () => cart.clear(),
              ),
            ],
          ),
        ),
        Expanded(
          child: cart.itemCount == 0
              ? const Center(child: Text('السلة فارغة'))
              : ListView.builder(
                  itemCount: cart.lines.length,
                  itemBuilder: (_, i) {
                    final line = cart.lines[i];
                    return Padding(
                      padding:
                          const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                      child: Card(
                        child: Padding(
                          padding: const EdgeInsets.all(8),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Expanded(
                                      child: Text(line.product.nameAr,
                                          style: const TextStyle(
                                              fontWeight: FontWeight.bold))),
                                  IconButton(
                                    icon: const Icon(Icons.close, size: 18),
                                    onPressed: () => cart.removeAt(i),
                                  ),
                                ],
                              ),
                              Row(
                                children: [
                                  IconButton(
                                    icon: const Icon(Icons.remove_circle_outline),
                                    onPressed: () => cart.updateQuantity(
                                        i, line.quantity - 1),
                                  ),
                                  Text('${line.quantity}',
                                      style: const TextStyle(fontSize: 16)),
                                  IconButton(
                                    icon: const Icon(Icons.add_circle_outline),
                                    onPressed: () => cart.updateQuantity(
                                        i, line.quantity + 1),
                                  ),
                                  const Spacer(),
                                  Text(Money.format(line.lineTotal),
                                      style: const TextStyle(
                                          fontWeight: FontWeight.bold)),
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
          color: Colors.grey.shade100,
          padding: const EdgeInsets.all(12),
          child: Column(
            children: [
              _summaryRow('المجموع', Money.format(cart.subTotal)),
              _summaryRow('الخصم', Money.format(
                  cart.lineDiscounts + cart.invoiceDiscount)),
              _summaryRow('ضريبة القيمة المضافة', Money.format(cart.vat)),
              const Divider(),
              _summaryRow('الإجمالي', Money.format(cart.total),
                  bold: true, size: 20),
              const SizedBox(height: 8),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  onPressed: cart.itemCount == 0
                      ? null
                      : () => Navigator.push(
                            context,
                            MaterialPageRoute(
                                builder: (_) => const CheckoutScreen()),
                          ),
                  icon: const Icon(Icons.payment),
                  label: const Text('الدفع'),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _summaryRow(String label, String value,
      {bool bold = false, double size = 14}) {
    final style = TextStyle(
      fontSize: size,
      fontWeight: bold ? FontWeight.bold : FontWeight.normal,
    );
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [Text(label, style: style), Text(value, style: style)],
      ),
    );
  }
}
