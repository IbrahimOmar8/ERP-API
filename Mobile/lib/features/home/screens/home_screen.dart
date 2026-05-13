import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../core/providers/auth_provider.dart';
import '../../inventory/screens/products_screen.dart';
import '../../inventory/screens/stock_screen.dart';
import '../../pos/screens/pos_screen.dart';
import '../../pos/screens/session_screen.dart';
import '../../pos/screens/sales_history_screen.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      appBar: AppBar(
        title: const Text('الرئيسية'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () => context.read<AuthProvider>().logout(),
          ),
        ],
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Card(
                color: Theme.of(context).colorScheme.primaryContainer,
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Row(
                    children: [
                      CircleAvatar(
                        backgroundColor: Theme.of(context).colorScheme.primary,
                        child: const Icon(Icons.person, color: Colors.white),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('مرحباً'),
                            Text(auth.userName,
                                style: const TextStyle(
                                    fontSize: 18, fontWeight: FontWeight.bold)),
                            if (auth.roles.isNotEmpty)
                              Text(auth.roles.join('، '),
                                  style: const TextStyle(
                                      fontSize: 12, color: Colors.black54)),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 24),
              const Text('نقاط البيع',
                  style:
                      TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              const SizedBox(height: 12),
              _GridMenu(items: [
                _MenuItem(Icons.point_of_sale, 'كاشير POS', Colors.green,
                    const PosScreen()),
                _MenuItem(Icons.cases_outlined, 'جلسات الكاش', Colors.orange,
                    const SessionScreen()),
                _MenuItem(Icons.receipt_long, 'الفواتير', Colors.blue,
                    const SalesHistoryScreen()),
              ]),
              const SizedBox(height: 24),
              const Text('المخازن',
                  style:
                      TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              const SizedBox(height: 12),
              _GridMenu(items: [
                _MenuItem(Icons.inventory_2, 'الأصناف', Colors.indigo,
                    const ProductsScreen()),
                _MenuItem(Icons.warehouse, 'الرصيد', Colors.teal,
                    const StockScreen()),
              ]),
            ],
          ),
        ),
      ),
    );
  }
}

class _MenuItem {
  final IconData icon;
  final String label;
  final Color color;
  final Widget screen;
  _MenuItem(this.icon, this.label, this.color, this.screen);
}

class _GridMenu extends StatelessWidget {
  final List<_MenuItem> items;
  const _GridMenu({required this.items});

  @override
  Widget build(BuildContext context) {
    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 3,
        crossAxisSpacing: 12,
        mainAxisSpacing: 12,
      ),
      itemCount: items.length,
      itemBuilder: (context, i) {
        final item = items[i];
        return InkWell(
          borderRadius: BorderRadius.circular(12),
          onTap: () => Navigator.push(
            context,
            MaterialPageRoute(builder: (_) => item.screen),
          ),
          child: Container(
            decoration: BoxDecoration(
              color: item.color.withOpacity(0.1),
              borderRadius: BorderRadius.circular(12),
            ),
            padding: const EdgeInsets.all(12),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(item.icon, size: 40, color: item.color),
                const SizedBox(height: 8),
                Text(item.label,
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                        fontSize: 13, fontWeight: FontWeight.w600)),
              ],
            ),
          ),
        );
      },
    );
  }
}
