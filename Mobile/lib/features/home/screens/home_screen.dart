import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../core/providers/auth_provider.dart';
import '../../../core/theme/app_theme.dart';
import '../../inventory/screens/products_screen.dart';
import '../../inventory/screens/stock_screen.dart';
import '../../inventory/screens/suppliers_screen.dart';
import '../../inventory/screens/transfers_screen.dart';
import '../../pos/screens/pos_screen.dart';
import '../../pos/screens/session_screen.dart';
import '../../pos/screens/sales_history_screen.dart';
import '../../reports/screens/dashboard_screen.dart';
import '../../reports/screens/sales_report_screen.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      body: CustomScrollView(
        slivers: [
          SliverToBoxAdapter(child: _GradientHeader(auth: auth)),
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _SectionTitle('نقاط البيع', icon: Icons.point_of_sale),
                  _Grid(items: [
                    _MenuItem(Icons.point_of_sale, 'كاشير POS',
                        AppTheme.success, const PosScreen()),
                    _MenuItem(Icons.cases_outlined, 'جلسات الكاش',
                        AppTheme.warn, const SessionScreen()),
                    _MenuItem(Icons.receipt_long, 'الفواتير',
                        AppTheme.primary, const SalesHistoryScreen()),
                  ]),
                  const SizedBox(height: 24),
                  _SectionTitle('المخازن', icon: Icons.inventory_2),
                  _Grid(items: [
                    _MenuItem(Icons.inventory_2, 'الأصناف',
                        const Color(0xFF3F51B5), const ProductsScreen()),
                    _MenuItem(Icons.warehouse, 'الرصيد',
                        AppTheme.accent, const StockScreen()),
                    _MenuItem(Icons.swap_horiz, 'تحويلات',
                        const Color(0xFF795548), const TransfersScreen()),
                    _MenuItem(Icons.local_shipping, 'الموردون',
                        const Color(0xFFFF7043), const SuppliersScreen()),
                  ]),
                  const SizedBox(height: 24),
                  _SectionTitle('التقارير', icon: Icons.analytics),
                  _Grid(items: [
                    _MenuItem(Icons.dashboard, 'لوحة التحكم',
                        const Color(0xFF673AB7), const DashboardScreen()),
                    _MenuItem(Icons.analytics, 'تقرير المبيعات',
                        const Color(0xFFE91E63), const SalesReportScreen()),
                  ]),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _GradientHeader extends StatelessWidget {
  final AuthProvider auth;
  const _GradientHeader({required this.auth});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
          colors: [AppTheme.primary, AppTheme.primaryDark],
        ),
        borderRadius: BorderRadius.only(
          bottomLeft: Radius.circular(28),
          bottomRight: Radius.circular(28),
        ),
      ),
      padding: const EdgeInsets.fromLTRB(20, 56, 20, 28),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 24,
                backgroundColor: Colors.white.withOpacity(0.2),
                child: Text(
                  auth.userName.isNotEmpty ? auth.userName[0].toUpperCase() : '?',
                  style: const TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('مرحباً 👋',
                        style: TextStyle(color: Colors.white70, fontSize: 13)),
                    Text(auth.userName,
                        style: const TextStyle(
                            color: Colors.white,
                            fontSize: 20,
                            fontWeight: FontWeight.bold)),
                    if (auth.roles.isNotEmpty)
                      Text(auth.roles.join(' · '),
                          style: const TextStyle(color: Colors.white70, fontSize: 12)),
                  ],
                ),
              ),
              IconButton(
                icon: const Icon(Icons.logout, color: Colors.white),
                onPressed: () => context.read<AuthProvider>().logout(),
                tooltip: 'خروج',
              ),
            ],
          ),
          const SizedBox(height: 16),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            decoration: BoxDecoration(
              color: Colors.white.withOpacity(0.18),
              borderRadius: BorderRadius.circular(12),
            ),
            child: const Row(
              children: [
                Icon(Icons.business, color: Colors.white, size: 18),
                SizedBox(width: 8),
                Text('نظام المخازن ونقاط البيع',
                    style: TextStyle(color: Colors.white)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  final String title;
  final IconData icon;
  const _SectionTitle(this.title, {required this.icon});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12, top: 4),
      child: Row(
        children: [
          Icon(icon, size: 18, color: Theme.of(context).colorScheme.primary),
          const SizedBox(width: 6),
          Text(title,
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }
}

class _Grid extends StatelessWidget {
  final List<_MenuItem> items;
  const _Grid({required this.items});

  @override
  Widget build(BuildContext context) {
    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 3,
        crossAxisSpacing: 10,
        mainAxisSpacing: 10,
        childAspectRatio: 0.95,
      ),
      itemCount: items.length,
      itemBuilder: (context, i) {
        final item = items[i];
        return InkWell(
          borderRadius: BorderRadius.circular(16),
          onTap: () => Navigator.push(
            context,
            MaterialPageRoute(builder: (_) => item.screen),
          ),
          child: Container(
            decoration: BoxDecoration(
              color: item.color.withOpacity(0.08),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: item.color.withOpacity(0.18)),
            ),
            padding: const EdgeInsets.all(12),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: item.color,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(item.icon, size: 26, color: Colors.white),
                ),
                const SizedBox(height: 8),
                Text(item.label,
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                        fontSize: 12.5, fontWeight: FontWeight.w600)),
              ],
            ),
          ),
        );
      },
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
