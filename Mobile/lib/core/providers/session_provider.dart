import 'package:flutter/foundation.dart';
import '../../features/pos/models/cash_session.dart';
import '../../features/pos/services/pos_service.dart';

class SessionProvider extends ChangeNotifier {
  CashSession? _current;
  String? _selectedWarehouseId;

  CashSession? get current => _current;
  String? get selectedWarehouseId => _selectedWarehouseId;
  bool get isOpen => _current?.status == 1;

  void setSession(CashSession? session) {
    _current = session;
    notifyListeners();
  }

  Future<void> loadCurrent(String userId) async {
    try {
      final session = await PosService.getCurrentSession(userId);
      _current = session;
      notifyListeners();
    } catch (_) {
      _current = null;
    }
  }

  void setWarehouse(String? id) {
    _selectedWarehouseId = id;
    notifyListeners();
  }

  void clear() {
    _current = null;
    notifyListeners();
  }
}
