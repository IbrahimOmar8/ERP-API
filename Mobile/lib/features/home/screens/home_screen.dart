import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../core/providers/auth_provider.dart';
import '../../../core/providers/theme_provider.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/utils/money.dart';
import '../../inventory/screens/products_screen.dart';
import '../../inventory/screens/stock_screen.dart';
import '../../inventory/screens/suppliers_screen.dart';
import '../../inventory/screens/transfers_screen.dart';
import '../../pos/screens/customers_screen.dart';
import '../../pos/screens/pos_screen.dart';
import '../../pos/screens/session_screen.dart';
import '../../pos/screens/sales_history_screen.dart';
import '../../notifications/widgets/notifications_bell.dart';
import '../../reports/models/dashboard_kpi.dart';
import '../../reports/screens/dashboard_screen.dart';
import '../../reports/screens/sales_report_screen.dart';
import '../../reports/services/report_service.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  DashboardKpi? _kpi;
  bool _kpiLoading = true;

  @override
  void initState() {
    super.initState();
    _loadKpi();
  }

  Future<void> _loadKpi() async {
    setState(() => _kpiLoading = true);
    try {
      _kpi = await ReportService.getDashboard();
    } catch (_) {
      // silent — KPI strip is optional
    } finally {
      if (mounted) setState(() => _kpiLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      body: RefreshIndicator(
        onRefresh: _loadKpi,
        child: CustomScrollView(
          slivers: [
            SliverToBoxAdapter(child: _GradientHeader(auth: auth)),
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Transform.translate(
                      offset: const Offset(0, -28),
                      child: _QuickStatsStrip(kpi: _kpi, loading: _kpiLoading),
                    ),
                    _SectionTitle('نقاط البيع', icon: Icons.point_of_sale),
                    _Grid(items: [
                      _MenuItem(Icons.point_of_sale, 'كاشير POS',
                          AppTheme.success, const PosScreen()),
                      _MenuItem(Icons.cases_outlined, 'جلسات الكاش',
                          AppTheme.warn, const SessionScreen()),
                      _MenuItem(Icons.receipt_long, 'الفواتير',
                          AppTheme.primary, const SalesHistoryScreen()),
                      _MenuItem(Icons.people, 'العملاء',
                          const Color(0xFF7E57C2), const CustomersScreen()),
                    ]),
                    const SizedBox(height: 20),
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
                    const SizedBox(height: 20),
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
      padding: const EdgeInsets.fromLTRB(20, 56, 20, 52),
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
                  style: const TextStyle(
                      color: Colors.white,
                      fontSize: 20,
                      fontWeight: FontWeight.bold),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('مرحباً 👋',
                        style:
                            TextStyle(color: Colors.white70, fontSize: 13)),
                    Text(auth.userName,
                        style: const TextStyle(
                            color: Colors.white,
                            fontSize: 20,
                            fontWeight: FontWeight.bold)),
                    if (auth.roles.isNotEmpty)
                      Text(auth.roles.join(' · '),
                          style: const TextStyle(
                              color: Colors.white70, fontSize: 12)),
                  ],
                ),
              ),
              const NotificationsBell(),
              Consumer<ThemeProvider>(
                builder: (_, theme, __) => IconButton(
                  icon: Icon(theme.isDark ? Icons.light_mode : Icons.dark_mode,
                      color: Colors.white),
                  tooltip: theme.isDark ? 'الوضع الفاتح' : 'الوضع الداكن',
                  onPressed: () => theme.toggle(),
                ),
              ),
              IconButton(
                icon: const Icon(Icons.logout, color: Colors.white),
                tooltip: 'خروج',
                onPressed: () => context.read<AuthProvider>().logout(),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _QuickStatsStrip extends StatelessWidget {
  final DashboardKpi? kpi;
  final bool loading;
  const _QuickStatsStrip({required this.kpi, required this.loading});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).cardTheme.color ?? Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(
          color: Theme.of(context).brightness == Brightness.dark
              ? const Color(0xFF334155)
              : const Color(0xFFE2E8F0),
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.06),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 14),
      child: loading
          ? Row(
              children: [
                Expanded(child: _StatPlaceholder()),
                _Divider(),
                Expanded(child: _StatPlaceholder()),
                _Divider(),
                Expanded(child: _StatPlaceholder()),
              ],
            )
          : kpi == null
              ? const Padding(
                  padding: EdgeInsets.symmetric(vertical: 8),
                  child: Text(
                    'تعذر تحميل المؤشرات السريعة',
                    textAlign: TextAlign.center,
                    style: TextStyle(color: Colors.grey),
                  ),
                )
              : Row(
                  children: [
                    Expanded(
                      child: _Stat(
                          label: 'مبيعات اليوم',
                          value: Money.format(kpi!.todaySales),
                          color: AppTheme.success,
                          icon: Icons.attach_money),
                    ),
                    _Divider(),
                    Expanded(
                      child: _Stat(
                          label: 'فواتير اليوم',
                          value: kpi!.todayInvoiceCount.toString(),
                          color: AppTheme.primary,
                          icon: Icons.receipt_long),
                    ),
                    _Divider(),
                    Expanded(
                      child: _Stat(
                          label: 'حد أدنى',
                          value: kpi!.lowStockCount.toString(),
                          color: AppTheme.warn,
                          icon: Icons.warning_amber),
                    ),
                  ],
                ),
    );
  }
}

class _Stat extends StatelessWidget {
  final String label;
  final String value;
  final Color color;
  final IconData icon;
  const _Stat({
    required this.label,
    required this.value,
    required this.color,
    required this.icon,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, color: color, size: 22),
        const SizedBox(height: 6),
        Text(
          value,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 2),
        Text(
          label,
          style: TextStyle(
            fontSize: 11,
            color: Theme.of(context).colorScheme.onSurface.withOpacity(0.6),
          ),
        ),
      ],
    );
  }
}

class _StatPlaceholder extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 22, height: 22,
          decoration: BoxDecoration(color: Colors.grey.shade300, shape: BoxShape.circle),
        ),
        const SizedBox(height: 8),
        Container(width: 50, height: 12, color: Colors.grey.shade300),
        const SizedBox(height: 4),
        Container(width: 30, height: 8, color: Colors.grey.shade300),
      ],
    );
  }
}

class _Divider extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      height: 40,
      width: 1,
      margin: const EdgeInsets.symmetric(horizontal: 4),
      color: Theme.of(context).dividerTheme.color ?? Colors.grey.shade300,
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
