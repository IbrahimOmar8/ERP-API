import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:uuid/uuid.dart';

class AuthProvider extends ChangeNotifier {
  bool _isAuthenticated = false;
  String _userName = '';
  String _userId = '';

  bool get isAuthenticated => _isAuthenticated;
  String get userName => _userName;
  String get userId => _userId;

  Future<void> login(String username, String password) async {
    // TODO: replace with real backend authentication
    await Future.delayed(const Duration(milliseconds: 500));
    final prefs = await SharedPreferences.getInstance();
    final storedId = prefs.getString('userId') ?? const Uuid().v4();
    await prefs.setString('userId', storedId);

    _userId = storedId;
    _userName = username;
    _isAuthenticated = true;
    notifyListeners();
  }

  void logout() {
    _isAuthenticated = false;
    _userName = '';
    _userId = '';
    notifyListeners();
  }
}
