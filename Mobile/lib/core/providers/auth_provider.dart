import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../features/auth/models/auth_models.dart';
import '../../features/auth/services/auth_service.dart';
import '../api/api_client.dart';

class AuthProvider extends ChangeNotifier {
  static const _kAccessToken = 'auth.accessToken';
  static const _kRefreshToken = 'auth.refreshToken';
  static const _kExpiresAt = 'auth.expiresAt';

  AppUser? _user;
  String? _accessToken;
  String? _refreshToken;
  DateTime? _expiresAt;
  bool _loading = false;

  bool get isAuthenticated => _user != null && _accessToken != null;
  bool get loading => _loading;
  AppUser? get user => _user;
  String get userId => _user?.id ?? '';
  String get userName => _user?.fullName ?? '';
  List<String> get roles => _user?.roles ?? [];

  Future<void> tryRestoreSession() async {
    final prefs = await SharedPreferences.getInstance();
    final access = prefs.getString(_kAccessToken);
    final refresh = prefs.getString(_kRefreshToken);
    final expIso = prefs.getString(_kExpiresAt);

    if (access == null || refresh == null) return;

    _accessToken = access;
    _refreshToken = refresh;
    _expiresAt = expIso != null ? DateTime.tryParse(expIso) : null;
    apiClient.setToken(_accessToken);

    try {
      _user = await AuthService.me();
      notifyListeners();
    } catch (_) {
      // Try refresh
      try {
        final res = await AuthService.refresh(refresh);
        await _persist(res);
      } catch (_) {
        await _clear();
      }
    }
  }

  Future<void> login(String userName, String password) async {
    _loading = true;
    notifyListeners();
    try {
      final res = await AuthService.login(userName, password);
      await _persist(res);
    } finally {
      _loading = false;
      notifyListeners();
    }
  }

  Future<void> logout() async {
    if (_refreshToken != null) {
      try {
        await AuthService.logout(_refreshToken!);
      } catch (_) {}
    }
    await _clear();
  }

  Future<void> _persist(TokenResponse res) async {
    _user = res.user;
    _accessToken = res.accessToken;
    _refreshToken = res.refreshToken;
    _expiresAt = res.expiresAt;
    apiClient.setToken(_accessToken);

    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_kAccessToken, res.accessToken);
    await prefs.setString(_kRefreshToken, res.refreshToken);
    await prefs.setString(_kExpiresAt, res.expiresAt.toIso8601String());
    notifyListeners();
  }

  Future<void> _clear() async {
    _user = null;
    _accessToken = null;
    _refreshToken = null;
    _expiresAt = null;
    apiClient.setToken(null);
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_kAccessToken);
    await prefs.remove(_kRefreshToken);
    await prefs.remove(_kExpiresAt);
    notifyListeners();
  }
}
