class ApiConfig {
  // Update this to your backend URL
  static const String baseUrl = 'http://10.0.2.2:5000/api';

  // Auth
  static const String authLogin = '/Auth/login';
  static const String authRefresh = '/Auth/refresh';
  static const String authLogout = '/Auth/logout';
  static const String authMe = '/Auth/me';
  static const String authChangePassword = '/Auth/change-password';

  // Endpoints
  static const String products = '/Products';
  static const String warehouses = '/Warehouses';
  static const String categories = '/Categories';
  static const String units = '/Units';
  static const String stock = '/Stock';
  static const String suppliers = '/Suppliers';
  static const String purchaseInvoices = '/PurchaseInvoices';

  static const String customers = '/Customers';
  static const String cashRegisters = '/CashRegisters';
  static const String cashSessions = '/CashSessions';
  static const String sales = '/Sales';
  static const String eInvoice = '/einvoice';
}
