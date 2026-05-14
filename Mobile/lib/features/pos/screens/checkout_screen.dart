import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../core/providers/auth_provider.dart';
import '../../../core/providers/cart_provider.dart';
import '../../../core/providers/session_provider.dart';
import '../../../core/utils/money.dart';
import '../services/pos_service.dart';
import 'receipt_screen.dart';

enum PayMethod { cash, card, instaPay, wallet, credit }

extension PayMethodX on PayMethod {
  int get backendValue {
    switch (this) {
      case PayMethod.cash: return 1;
      case PayMethod.card: return 2;
      case PayMethod.instaPay: return 3;
      case PayMethod.wallet: return 4;
      case PayMethod.credit: return 6;
    }
  }

  String get label {
    switch (this) {
      case PayMethod.cash: return 'كاش';
      case PayMethod.card: return 'بطاقة';
      case PayMethod.instaPay: return 'InstaPay';
      case PayMethod.wallet: return 'محفظة';
      case PayMethod.credit: return 'آجل';
    }
  }

  IconData get icon {
    switch (this) {
      case PayMethod.cash: return Icons.payments;
      case PayMethod.card: return Icons.credit_card;
      case PayMethod.instaPay: return Icons.bolt;
      case PayMethod.wallet: return Icons.account_balance_wallet;
      case PayMethod.credit: return Icons.account_balance;
    }
  }

  Color get color {
    switch (this) {
      case PayMethod.cash: return Colors.green;
      case PayMethod.card: return Colors.blue;
      case PayMethod.instaPay: return Colors.purple;
      case PayMethod.wallet: return Colors.orange;
      case PayMethod.credit: return Colors.grey;
    }
  }
}

class CheckoutScreen extends StatefulWidget {
  const CheckoutScreen({super.key});

  @override
  State<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends State<CheckoutScreen> {
  PayMethod _method = PayMethod.cash;
  final _amountController = TextEditingController();
  bool _submitting = false;

  @override
  void dispose() {
    _amountController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final cart = context.watch<CartProvider>();
    final paid = double.tryParse(_amountController.text) ?? cart.total;
    final change = paid - cart.total;
    final primary = Theme.of(context).colorScheme.primary;

    return Scaffold(
      appBar: AppBar(title: const Text('إتمام الدفع')),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: ElevatedButton.icon(
            onPressed: _submitting || cart.itemCount == 0 ? null : _submit,
            icon: _submitting
                ? const SizedBox(
                    height: 18,
                    width: 18,
                    child: CircularProgressIndicator(
                        strokeWidth: 2, color: Colors.white))
                : const Icon(Icons.check_circle),
            label: Text(_submitting
                ? 'جاري الحفظ...'
                : 'تأكيد ودفع ${Money.format(cart.total)}'),
            style: ElevatedButton.styleFrom(
              padding: const EdgeInsets.symmetric(vertical: 16),
              textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
          ),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Total hero card
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  colors: [primary, primary.withOpacity(0.7)],
                  begin: Alignment.topRight,
                  end: Alignment.bottomLeft,
                ),
                borderRadius: BorderRadius.circular(20),
              ),
              child: Column(
                children: [
                  const Text('الإجمالي المستحق',
                      style: TextStyle(color: Colors.white70, fontSize: 14)),
                  const SizedBox(height: 6),
                  Text(
                    Money.format(cart.total),
                    style: const TextStyle(
                        color: Colors.white,
                        fontSize: 32,
                        fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 6),
                  Text('${cart.itemCount} صنف',
                      style:
                          const TextStyle(color: Colors.white70, fontSize: 12)),
                ],
              ),
            ),
            const SizedBox(height: 16),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: [
                    _row('المجموع', Money.format(cart.subTotal)),
                    _row(
                      'الخصم',
                      Money.format(cart.lineDiscounts + cart.invoiceDiscount),
                      color: Colors.red,
                    ),
                    _row('ضريبة 14%', Money.format(cart.vat)),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            const Text('طريقة الدفع',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
            const SizedBox(height: 10),
            GridView.count(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              crossAxisCount: 3,
              crossAxisSpacing: 8,
              mainAxisSpacing: 8,
              childAspectRatio: 1.5,
              children: PayMethod.values.map((m) {
                final selected = _method == m;
                return InkWell(
                  onTap: () => setState(() => _method = m),
                  borderRadius: BorderRadius.circular(12),
                  child: Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: selected
                          ? m.color.withOpacity(0.15)
                          : Theme.of(context).cardTheme.color,
                      border: Border.all(
                        color: selected ? m.color : Colors.grey.shade300,
                        width: selected ? 2 : 1,
                      ),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(m.icon, color: m.color, size: 22),
                        const SizedBox(height: 6),
                        Text(m.label,
                            style: TextStyle(
                                fontSize: 12,
                                fontWeight: selected
                                    ? FontWeight.bold
                                    : FontWeight.normal,
                                color: selected ? m.color : null)),
                      ],
                    ),
                  ),
                );
              }).toList(),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _amountController,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
              decoration: InputDecoration(
                labelText: 'المبلغ المستلم',
                hintText: Money.format(cart.total),
                prefixIcon: const Icon(Icons.attach_money),
                suffixIcon: _amountController.text.isEmpty
                    ? null
                    : IconButton(
                        icon: const Icon(Icons.close),
                        onPressed: () =>
                            setState(() => _amountController.clear()),
                      ),
              ),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 8),
            // Quick amounts
            Wrap(
              spacing: 6,
              children: [
                _quickAmount(cart.total),
                _quickAmount((cart.total / 50).ceil() * 50.0),
                _quickAmount((cart.total / 100).ceil() * 100.0),
                _quickAmount((cart.total / 500).ceil() * 500.0),
              ],
            ),
            if (_method == PayMethod.cash && _amountController.text.isNotEmpty) ...[
              const SizedBox(height: 12),
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: change >= 0
                      ? Colors.green.withOpacity(0.1)
                      : Colors.red.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(
                      color: change >= 0
                          ? Colors.green.shade300
                          : Colors.red.shade300),
                ),
                child: Row(
                  children: [
                    Icon(
                      change >= 0 ? Icons.arrow_back : Icons.warning,
                      color: change >= 0 ? Colors.green : Colors.red,
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        change >= 0
                            ? 'الباقي للعميل: ${Money.format(change)}'
                            : 'المبلغ المستلم أقل من المطلوب بـ ${Money.format(-change)}',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          color: change >= 0 ? Colors.green : Colors.red,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _quickAmount(double amount) {
    return ActionChip(
      label: Text(Money.format(amount)),
      onPressed: () => setState(() => _amountController.text = amount.toStringAsFixed(2)),
    );
  }

  Future<void> _submit() async {
    final cart = context.read<CartProvider>();
    final auth = context.read<AuthProvider>();
    final session = context.read<SessionProvider>();

    if (session.current == null) {
      ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('لا توجد جلسة كاش مفتوحة')));
      return;
    }

    final paidAmount = double.tryParse(_amountController.text) ?? cart.total;

    setState(() => _submitting = true);
    try {
      final sale = await PosService.createSale(
        cashierUserId: auth.userId,
        customerId: cart.customerId,
        warehouseId: session.current!.warehouseId,
        cashSessionId: session.current!.id,
        items: cart.lines
            .map((l) => {
                  'productId': l.product.id,
                  'quantity': l.quantity,
                  'unitPrice': l.product.salePrice,
                  'discountAmount': l.discountAmount,
                  'discountPercent': l.discountPercent,
                })
            .toList(),
        payments: [
          {'method': _method.backendValue, 'amount': paidAmount},
        ],
        discountAmount: cart.discountAmount,
        discountPercent: cart.discountPercent,
      );
      cart.clear();
      if (!mounted) return;
      Navigator.pushReplacement(context,
          MaterialPageRoute(builder: (_) => ReceiptScreen(sale: sale)));
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  Widget _row(String label, String value, {Color? color}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label),
            Text(value,
                style:
                    TextStyle(fontWeight: FontWeight.bold, color: color)),
          ],
        ),
      );
}
