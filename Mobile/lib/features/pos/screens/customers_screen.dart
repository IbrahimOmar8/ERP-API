import 'package:flutter/material.dart';
import '../../../core/utils/money.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/loading_shimmer.dart';
import '../models/customer.dart';
import '../services/pos_service.dart';

class CustomersScreen extends StatefulWidget {
  const CustomersScreen({super.key});

  @override
  State<CustomersScreen> createState() => _CustomersScreenState();
}

class _CustomersScreenState extends State<CustomersScreen> {
  List<Customer> _items = [];
  bool _loading = true;
  String _search = '';

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      _items = await PosService.getCustomers(
          search: _search.isEmpty ? null : _search);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _addCustomer() async {
    final result = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => const _CustomerForm(),
    );
    if (result == true) _load();
  }

  @override
  Widget build(BuildContext context) {
    final companies = _items.where((c) => c.isCompany).length;
    final individuals = _items.length - companies;
    return Scaffold(
      appBar: AppBar(
        title: const Text('العملاء'),
        actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: _load)],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _addCustomer,
        icon: const Icon(Icons.add),
        label: const Text('عميل جديد'),
      ),
      body: Column(
        children: [
          if (_items.isNotEmpty)
            Padding(
              padding: const EdgeInsets.fromLTRB(12, 12, 12, 0),
              child: Row(
                children: [
                  Expanded(
                    child: _tile(
                      icon: Icons.person,
                      color: Colors.indigo,
                      label: 'أفراد',
                      value: individuals.toString(),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _tile(
                      icon: Icons.business,
                      color: Colors.teal,
                      label: 'شركات',
                      value: companies.toString(),
                    ),
                  ),
                ],
              ),
            ),
          Padding(
            padding: const EdgeInsets.all(12),
            child: TextField(
              decoration: const InputDecoration(
                prefixIcon: Icon(Icons.search),
                hintText: 'ابحث بالاسم أو الهاتف...',
              ),
              onChanged: (v) {
                setState(() => _search = v);
                _load();
              },
            ),
          ),
          Expanded(
            child: _loading
                ? const LoadingShimmerList(itemHeight: 72)
                : _items.isEmpty
                    ? EmptyState(
                        icon: Icons.people_outline,
                        message: _search.isEmpty
                            ? 'لا يوجد عملاء بعد'
                            : 'لا توجد نتائج',
                        actionLabel: _search.isEmpty ? 'إضافة عميل' : null,
                        onAction: _search.isEmpty ? _addCustomer : null,
                      )
                    : RefreshIndicator(
                        onRefresh: _load,
                        child: ListView.separated(
                          padding: const EdgeInsets.fromLTRB(12, 0, 12, 80),
                          itemCount: _items.length,
                          separatorBuilder: (_, __) =>
                              const SizedBox(height: 6),
                          itemBuilder: (_, i) {
                            final c = _items[i];
                            final color =
                                c.isCompany ? Colors.teal : Colors.indigo;
                            return Card(
                              child: ListTile(
                                leading: CircleAvatar(
                                  backgroundColor: color.withOpacity(0.12),
                                  child: Icon(
                                    c.isCompany ? Icons.business : Icons.person,
                                    color: color,
                                  ),
                                ),
                                title: Text(c.name,
                                    style: const TextStyle(
                                        fontWeight: FontWeight.w600)),
                                subtitle: Text(
                                  [
                                    c.phone,
                                    c.taxRegistrationNumber,
                                    c.nationalId,
                                  ]
                                      .whereType<String>()
                                      .where((s) => s.isNotEmpty)
                                      .join(' · '),
                                  style: const TextStyle(fontSize: 12),
                                ),
                                trailing: Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  crossAxisAlignment: CrossAxisAlignment.end,
                                  children: [
                                    const Text('الرصيد',
                                        style: TextStyle(
                                            fontSize: 10, color: Colors.grey)),
                                    Text(Money.format(c.balance),
                                        style: TextStyle(
                                            fontWeight: FontWeight.bold,
                                            color: c.balance > 0
                                                ? Colors.orange
                                                : Colors.green)),
                                  ],
                                ),
                              ),
                            );
                          },
                        ),
                      ),
          ),
        ],
      ),
    );
  }

  Widget _tile({
    required IconData icon,
    required Color color,
    required String label,
    required String value,
  }) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: color.withOpacity(0.08),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withOpacity(0.18)),
      ),
      child: Row(
        children: [
          Icon(icon, color: color, size: 20),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(label,
                    style: const TextStyle(fontSize: 11, color: Colors.grey)),
                Text(value,
                    style: const TextStyle(
                        fontSize: 17, fontWeight: FontWeight.bold)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _CustomerForm extends StatefulWidget {
  const _CustomerForm();

  @override
  State<_CustomerForm> createState() => _CustomerFormState();
}

class _CustomerFormState extends State<_CustomerForm> {
  final _name = TextEditingController();
  final _phone = TextEditingController();
  final _email = TextEditingController();
  final _address = TextEditingController();
  final _trn = TextEditingController();
  final _nationalId = TextEditingController();
  bool _isCompany = false;
  bool _saving = false;

  @override
  void dispose() {
    _name.dispose();
    _phone.dispose();
    _email.dispose();
    _address.dispose();
    _trn.dispose();
    _nationalId.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_name.text.trim().isEmpty) return;
    setState(() => _saving = true);
    try {
      await PosService.createCustomer({
        'name': _name.text.trim(),
        'phone': _phone.text.trim().isEmpty ? null : _phone.text.trim(),
        'email': _email.text.trim().isEmpty ? null : _email.text.trim(),
        'address': _address.text.trim().isEmpty ? null : _address.text.trim(),
        'taxRegistrationNumber':
            _trn.text.trim().isEmpty ? null : _trn.text.trim(),
        'nationalId':
            _nationalId.text.trim().isEmpty ? null : _nationalId.text.trim(),
        'isCompany': _isCompany,
      });
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(
        bottom: MediaQuery.of(context).viewInsets.bottom,
        top: 16,
        left: 16,
        right: 16,
      ),
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: Colors.grey.shade400,
                  borderRadius: BorderRadius.circular(4),
                ),
              ),
            ),
            const SizedBox(height: 12),
            const Center(
              child: Text('عميل جديد',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            ),
            const SizedBox(height: 16),
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              title: Text(_isCompany ? 'شركة (B2B)' : 'فرد'),
              subtitle: const Text(
                  'فعّل للشركات لاستخدام الرقم الضريبي في الفاتورة الإلكترونية'),
              value: _isCompany,
              onChanged: (v) => setState(() => _isCompany = v),
            ),
            const SizedBox(height: 8),
            TextField(
                controller: _name,
                decoration: const InputDecoration(
                    labelText: 'الاسم *',
                    prefixIcon: Icon(Icons.person_outline))),
            const SizedBox(height: 10),
            TextField(
                controller: _phone,
                keyboardType: TextInputType.phone,
                decoration: const InputDecoration(
                    labelText: 'الهاتف', prefixIcon: Icon(Icons.phone))),
            const SizedBox(height: 10),
            TextField(
                controller: _email,
                keyboardType: TextInputType.emailAddress,
                decoration: const InputDecoration(
                    labelText: 'البريد', prefixIcon: Icon(Icons.email))),
            const SizedBox(height: 10),
            if (_isCompany)
              TextField(
                  controller: _trn,
                  decoration: const InputDecoration(
                      labelText: 'الرقم الضريبي',
                      prefixIcon: Icon(Icons.confirmation_number)))
            else
              TextField(
                  controller: _nationalId,
                  decoration: const InputDecoration(
                      labelText: 'الرقم القومي',
                      prefixIcon: Icon(Icons.badge))),
            const SizedBox(height: 10),
            TextField(
                controller: _address,
                decoration: const InputDecoration(
                    labelText: 'العنوان', prefixIcon: Icon(Icons.location_on))),
            const SizedBox(height: 16),
            ElevatedButton.icon(
              onPressed: _saving ? null : _save,
              icon: _saving
                  ? const SizedBox(
                      height: 18,
                      width: 18,
                      child: CircularProgressIndicator(
                          strokeWidth: 2, color: Colors.white),
                    )
                  : const Icon(Icons.save),
              label: Text(_saving ? 'جاري الحفظ...' : 'حفظ'),
              style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14)),
            ),
            const SizedBox(height: 16),
          ],
        ),
      ),
    );
  }
}
