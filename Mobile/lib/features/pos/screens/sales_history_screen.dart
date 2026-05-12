import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../core/providers/auth_provider.dart';
import '../../../core/utils/money.dart';
import '../models/sale.dart';
import '../services/pos_service.dart';
import 'receipt_screen.dart';

class SalesHistoryScreen extends StatefulWidget {
  const SalesHistoryScreen({super.key});

  @override
  State<SalesHistoryScreen> createState() => _SalesHistoryScreenState();
}

class _SalesHistoryScreenState extends State<SalesHistoryScreen> {
  List<Sale> _sales = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final auth = context.read<AuthProvider>();
      _sales = await PosService.getSales(cashierId: auth.userId);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('سجل الفواتير'),
        actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: _load)],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _sales.isEmpty
              ? const Center(child: Text('لا توجد فواتير'))
              : ListView.separated(
                  itemCount: _sales.length,
                  separatorBuilder: (_, __) => const Divider(height: 1),
                  itemBuilder: (_, i) {
                    final s = _sales[i];
                    return ListTile(
                      leading: CircleAvatar(
                        backgroundColor:
                            _statusColor(s.status).withOpacity(0.15),
                        child: Icon(Icons.receipt,
                            color: _statusColor(s.status), size: 20),
                      ),
                      title: Text(s.invoiceNumber),
                      subtitle: Text(
                          '${s.saleDate.toLocal()} • ${s.customerName ?? "عميل نقدي"}'),
                      trailing: Column(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Text(Money.format(s.total),
                              style:
                                  const TextStyle(fontWeight: FontWeight.bold)),
                          if (s.eInvoiceUuid != null)
                            const Text('ETA',
                                style: TextStyle(
                                    fontSize: 10, color: Colors.green)),
                        ],
                      ),
                      onTap: () => Navigator.push(
                          context,
                          MaterialPageRoute(
                              builder: (_) => ReceiptScreen(sale: s))),
                    );
                  },
                ),
    );
  }

  Color _statusColor(int status) {
    switch (status) {
      case 1: return Colors.green;
      case 2: return Colors.grey;
      case 3: return Colors.red;
      case 4: return Colors.orange;
      default: return Colors.grey;
    }
  }
}
