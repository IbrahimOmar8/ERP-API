import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../../../core/providers/auth_provider.dart';
import '../../../core/providers/session_provider.dart';
import '../../../core/utils/money.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/loading_shimmer.dart';
import '../models/cash_session.dart';
import '../services/pos_service.dart';

class SessionScreen extends StatefulWidget {
  const SessionScreen({super.key});

  @override
  State<SessionScreen> createState() => _SessionScreenState();
}

class _SessionScreenState extends State<SessionScreen> {
  List<CashRegister> _registers = [];
  CashRegister? _selected;
  bool _loading = true;
  bool _busy = false;
  final _openingBalance = TextEditingController(text: '0');
  final _closingBalance = TextEditingController(text: '0');
  final _notes = TextEditingController();

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _openingBalance.dispose();
    _closingBalance.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final auth = context.read<AuthProvider>();
      final session = context.read<SessionProvider>();
      await session.loadCurrent(auth.userId);
      _registers = await PosService.getCashRegisters();
      if (_registers.isNotEmpty) _selected = _registers.first;
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _open() async {
    if (_selected == null) return;
    setState(() => _busy = true);
    final auth = context.read<AuthProvider>();
    final session = context.read<SessionProvider>();
    try {
      final s = await PosService.openSession(auth.userId, _selected!.id,
          double.tryParse(_openingBalance.text) ?? 0);
      session.setSession(s);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('تم فتح الجلسة بنجاح')));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _close() async {
    final session = context.read<SessionProvider>();
    if (session.current == null) return;
    setState(() => _busy = true);
    try {
      final closed = await PosService.closeSession(
        session.current!.id,
        double.tryParse(_closingBalance.text) ?? 0,
        notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(),
      );
      session.setSession(closed);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('تم إغلاق الجلسة')));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = context.watch<SessionProvider>();
    return Scaffold(
      appBar: AppBar(
        title: const Text('جلسة الكاش'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _load),
        ],
      ),
      body: _loading
          ? const Padding(
              padding: EdgeInsets.all(12),
              child: LoadingShimmerList(itemCount: 3, itemHeight: 120),
            )
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child:
                  session.isOpen ? _openCard(session.current!) : _openForm(),
            ),
    );
  }

  Widget _openForm() {
    if (_registers.isEmpty) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 40),
        child: EmptyState(
          icon: Icons.point_of_sale,
          message: 'لا توجد ماكينات كاشير مسجلة',
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: Colors.amber.shade50,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: Colors.amber.shade200),
          ),
          child: Row(
            children: [
              Icon(Icons.info_outline, color: Colors.amber.shade800),
              const SizedBox(width: 8),
              const Expanded(
                child: Text(
                  'لا توجد جلسة مفتوحة. اختر الماكينة وافتح جلسة لبدء البيع.',
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        DropdownButtonFormField<CashRegister>(
          value: _selected,
          decoration: const InputDecoration(
            labelText: 'الكاشير',
            prefixIcon: Icon(Icons.point_of_sale),
          ),
          items: _registers
              .map((r) => DropdownMenuItem(
                  value: r, child: Text('${r.name} (${r.code})')))
              .toList(),
          onChanged: (r) => setState(() => _selected = r),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: _openingBalance,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: const InputDecoration(
            labelText: 'رصيد الافتتاح (ج.م)',
            prefixIcon: Icon(Icons.account_balance_wallet),
          ),
        ),
        const SizedBox(height: 20),
        ElevatedButton.icon(
          icon: _busy
              ? const SizedBox(
                  height: 18,
                  width: 18,
                  child: CircularProgressIndicator(
                      strokeWidth: 2, color: Colors.white))
              : const Icon(Icons.lock_open),
          label: Text(_busy ? 'جاري الفتح...' : 'فتح الجلسة'),
          onPressed: _busy ? null : _open,
          style: ElevatedButton.styleFrom(
            padding: const EdgeInsets.symmetric(vertical: 14),
          ),
        ),
      ],
    );
  }

  Widget _openCard(CashSession s) {
    final df = DateFormat('yyyy-MM-dd HH:mm');
    final expectedDiff =
        (double.tryParse(_closingBalance.text) ?? 0) - s.expectedBalance;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            gradient: LinearGradient(
              colors: [Colors.green.shade400, Colors.green.shade600],
            ),
            borderRadius: BorderRadius.circular(16),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: Colors.white.withOpacity(0.2),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: const Icon(Icons.check_circle,
                        color: Colors.white, size: 22),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('جلسة مفتوحة',
                            style: TextStyle(color: Colors.white70, fontSize: 12)),
                        Text(s.cashRegisterName ?? '—',
                            style: const TextStyle(
                                color: Colors.white,
                                fontSize: 17,
                                fontWeight: FontWeight.bold)),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Text('فُتحت في: ${df.format(s.openedAt.toLocal())}',
                  style: const TextStyle(color: Colors.white70, fontSize: 12)),
            ],
          ),
        ),
        const SizedBox(height: 16),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              children: [
                _row('رصيد الافتتاح', Money.format(s.openingBalance)),
                const Divider(),
                _row('مبيعات نقدي', Money.format(s.totalCashSales),
                    color: Colors.green),
                _row('مبيعات بطاقات', Money.format(s.totalCardSales),
                    color: Colors.blue),
                _row('مبيعات أخرى', Money.format(s.totalOtherSales),
                    color: Colors.purple),
                _row('مرتجعات', Money.format(s.totalRefunds),
                    color: Colors.red),
                const Divider(),
                _row('الرصيد المتوقع', Money.format(s.expectedBalance),
                    bold: true),
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        TextField(
          controller: _closingBalance,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: const InputDecoration(
            labelText: 'رصيد الإقفال الفعلي (ج.م)',
            prefixIcon: Icon(Icons.account_balance_wallet),
          ),
          onChanged: (_) => setState(() {}),
        ),
        if (double.tryParse(_closingBalance.text) != null) ...[
          const SizedBox(height: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            decoration: BoxDecoration(
              color: expectedDiff == 0
                  ? Colors.green.shade50
                  : expectedDiff < 0
                      ? Colors.red.shade50
                      : Colors.amber.shade50,
              borderRadius: BorderRadius.circular(10),
            ),
            child: Row(
              children: [
                Icon(
                  expectedDiff == 0
                      ? Icons.check_circle
                      : expectedDiff < 0
                          ? Icons.remove_circle
                          : Icons.add_circle,
                  color: expectedDiff == 0
                      ? Colors.green
                      : expectedDiff < 0
                          ? Colors.red
                          : Colors.orange,
                  size: 18,
                ),
                const SizedBox(width: 8),
                Text('الفرق: ${Money.format(expectedDiff)}'),
              ],
            ),
          ),
        ],
        const SizedBox(height: 12),
        TextField(
          controller: _notes,
          decoration: const InputDecoration(
            labelText: 'ملاحظات الإقفال (اختياري)',
            prefixIcon: Icon(Icons.note),
          ),
        ),
        const SizedBox(height: 20),
        ElevatedButton.icon(
          style: ElevatedButton.styleFrom(
            backgroundColor: Colors.redAccent,
            padding: const EdgeInsets.symmetric(vertical: 14),
          ),
          icon: _busy
              ? const SizedBox(
                  height: 18,
                  width: 18,
                  child: CircularProgressIndicator(
                      strokeWidth: 2, color: Colors.white))
              : const Icon(Icons.lock_outline),
          label: Text(_busy ? 'جاري الإغلاق...' : 'إغلاق الجلسة'),
          onPressed: _busy ? null : _close,
        ),
      ],
    );
  }

  Widget _row(String label, String value,
      {bool bold = false, Color? color}) =>
      Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label,
                style: TextStyle(
                    fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
            Text(value,
                style: TextStyle(
                    fontWeight: FontWeight.bold,
                    color: color,
                    fontSize: bold ? 16 : 14)),
          ],
        ),
      );
}
