import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../core/providers/auth_provider.dart';
import '../../../core/providers/session_provider.dart';
import '../../../core/utils/money.dart';
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
  final _openingBalance = TextEditingController(text: '0');
  final _closingBalance = TextEditingController(text: '0');

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final auth = context.read<AuthProvider>();
      final session = context.read<SessionProvider>();
      await session.loadCurrent(auth.userId);
      _registers = await PosService.getCashRegisters();
      if (_registers.isNotEmpty) {
        _selected = _registers.first;
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _open() async {
    if (_selected == null) return;
    final auth = context.read<AuthProvider>();
    final session = context.read<SessionProvider>();
    try {
      final s = await PosService.openSession(
          auth.userId, _selected!.id, double.tryParse(_openingBalance.text) ?? 0);
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
    }
  }

  Future<void> _close() async {
    final session = context.read<SessionProvider>();
    if (session.current == null) return;
    try {
      final closed = await PosService.closeSession(
          session.current!.id, double.tryParse(_closingBalance.text) ?? 0);
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
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = context.watch<SessionProvider>();
    return Scaffold(
      appBar: AppBar(title: const Text('جلسة الكاش')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: session.isOpen
                  ? _openCard(session.current!)
                  : _openForm(),
            ),
    );
  }

  Widget _openForm() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Card(
          color: Color(0xFFFFF3E0),
          child: Padding(
            padding: EdgeInsets.all(16),
            child: Text('لا توجد جلسة مفتوحة. افتح جلسة لبدء البيع.',
                textAlign: TextAlign.center),
          ),
        ),
        const SizedBox(height: 16),
        DropdownButtonFormField<CashRegister>(
          value: _selected,
          decoration: const InputDecoration(labelText: 'اختر الكاشير'),
          items: _registers
              .map((r) =>
                  DropdownMenuItem(value: r, child: Text('${r.name} (${r.code})')))
              .toList(),
          onChanged: (r) => setState(() => _selected = r),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: _openingBalance,
          keyboardType: TextInputType.number,
          decoration: const InputDecoration(labelText: 'رصيد الافتتاح (ج.م)'),
        ),
        const SizedBox(height: 16),
        ElevatedButton.icon(
          icon: const Icon(Icons.lock_open),
          label: const Text('فتح الجلسة'),
          onPressed: _open,
        ),
      ],
    );
  }

  Widget _openCard(CashSession s) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Card(
          color: const Color(0xFFE8F5E9),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('${s.cashRegisterName ?? "—"}',
                    style: const TextStyle(
                        fontSize: 18, fontWeight: FontWeight.bold)),
                Text('فُتحت: ${s.openedAt.toLocal()}'),
                const Divider(),
                _row('رصيد الافتتاح', Money.format(s.openingBalance)),
                _row('مبيعات كاش', Money.format(s.totalCashSales)),
                _row('مبيعات بطاقات', Money.format(s.totalCardSales)),
                _row('الرصيد المتوقع', Money.format(s.expectedBalance)),
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        TextField(
          controller: _closingBalance,
          keyboardType: TextInputType.number,
          decoration: const InputDecoration(labelText: 'رصيد الإقفال (ج.م)'),
        ),
        const SizedBox(height: 16),
        ElevatedButton.icon(
          style: ElevatedButton.styleFrom(backgroundColor: Colors.redAccent),
          icon: const Icon(Icons.lock_outline),
          label: const Text('إغلاق الجلسة'),
          onPressed: _close,
        ),
      ],
    );
  }

  Widget _row(String label, String value) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [Text(label), Text(value, style: const TextStyle(fontWeight: FontWeight.bold))],
        ),
      );
}
