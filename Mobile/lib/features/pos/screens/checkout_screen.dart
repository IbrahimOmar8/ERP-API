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
  Widget build(BuildContext context) {
    final cart = context.watch<CartProvider>();
    return Scaffold(
      appBar: AppBar(title: const Text('إتمام الدفع')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: [
                    _row('عدد الأصناف', '${cart.itemCount}'),
                    _row('المجموع', Money.format(cart.subTotal)),
                    _row('الخصم', Money.format(
                        cart.lineDiscounts + cart.invoiceDiscount)),
                    _row('ضريبة 14%', Money.format(cart.vat)),
                    const Divider(),
                    _row('الإجمالي للدفع', Money.format(cart.total),
                        bold: true, size: 20),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            const Text('طريقة الدفع',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: PayMethod.values.map((m) {
                return ChoiceChip(
                  selected: _method == m,
                  label: Text(m.label),
                  onSelected: (_) => setState(() => _method = m),
                );
              }).toList(),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _amountController,
              keyboardType: TextInputType.number,
              decoration: InputDecoration(
                labelText: 'المبلغ المستلم',
                hintText: Money.format(cart.total),
              ),
            ),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: _submitting ? null : _submit,
              icon: const Icon(Icons.check_circle),
              label: _submitting
                  ? const Text('جاري الحفظ...')
                  : const Text('حفظ الفاتورة'),
            ),
          ],
        ),
      ),
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
          {
            'method': _method.backendValue,
            'amount': paidAmount,
          },
        ],
        discountAmount: cart.discountAmount,
        discountPercent: cart.discountPercent,
      );
      cart.clear();
      if (!mounted) return;
      Navigator.pushReplacement(
          context,
          MaterialPageRoute(
              builder: (_) => ReceiptScreen(sale: sale)));
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  Widget _row(String label, String value,
      {bool bold = false, double size = 14}) {
    final style = TextStyle(
        fontSize: size, fontWeight: bold ? FontWeight.bold : FontWeight.normal);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [Text(label, style: style), Text(value, style: style)],
      ),
    );
  }
}
