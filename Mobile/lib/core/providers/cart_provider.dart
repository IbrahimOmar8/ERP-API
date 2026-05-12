import 'package:flutter/foundation.dart';
import '../../features/inventory/models/product.dart';

class CartLine {
  final Product product;
  double quantity;
  double discountAmount;
  double discountPercent;

  CartLine({
    required this.product,
    this.quantity = 1,
    this.discountAmount = 0,
    this.discountPercent = 0,
  });

  double get lineSubBefore => quantity * product.salePrice;
  double get totalDiscount => discountAmount + (lineSubBefore * discountPercent / 100);
  double get lineSub => lineSubBefore - totalDiscount;
  double get vatAmount => lineSub * (product.vatRate / 100);
  double get lineTotal => lineSub + vatAmount;
}

class CartProvider extends ChangeNotifier {
  final List<CartLine> _lines = [];
  double _discountAmount = 0;
  double _discountPercent = 0;
  String? _customerId;
  String? _customerName;

  List<CartLine> get lines => List.unmodifiable(_lines);
  double get discountAmount => _discountAmount;
  double get discountPercent => _discountPercent;
  String? get customerId => _customerId;
  String? get customerName => _customerName;

  double get subTotal => _lines.fold(0, (s, l) => s + l.lineSubBefore);
  double get lineDiscounts => _lines.fold(0, (s, l) => s + l.totalDiscount);
  double get invoiceDiscount =>
      _discountAmount + (subTotal * _discountPercent / 100);
  double get vat => _lines.fold(0, (s, l) => s + l.vatAmount);
  double get total => subTotal - lineDiscounts - invoiceDiscount + vat;
  int get itemCount => _lines.length;

  void addProduct(Product product, {double quantity = 1}) {
    final existing = _lines.indexWhere((l) => l.product.id == product.id);
    if (existing >= 0) {
      _lines[existing].quantity += quantity;
    } else {
      _lines.add(CartLine(product: product, quantity: quantity));
    }
    notifyListeners();
  }

  void updateQuantity(int index, double quantity) {
    if (quantity <= 0) {
      _lines.removeAt(index);
    } else {
      _lines[index].quantity = quantity;
    }
    notifyListeners();
  }

  void updateDiscount(int index, double percent) {
    _lines[index].discountPercent = percent;
    notifyListeners();
  }

  void removeAt(int index) {
    _lines.removeAt(index);
    notifyListeners();
  }

  void setCustomer(String? id, String? name) {
    _customerId = id;
    _customerName = name;
    notifyListeners();
  }

  void setInvoiceDiscount({double amount = 0, double percent = 0}) {
    _discountAmount = amount;
    _discountPercent = percent;
    notifyListeners();
  }

  void clear() {
    _lines.clear();
    _discountAmount = 0;
    _discountPercent = 0;
    _customerId = null;
    _customerName = null;
    notifyListeners();
  }
}
