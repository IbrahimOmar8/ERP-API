import '../../../core/api/api_client.dart';
import '../../../core/config/api_config.dart';
import '../models/auth_models.dart';

class AuthService {
  static Future<TokenResponse> login(String userName, String password) async {
    final data = await apiClient.post(ApiConfig.authLogin, {
      'userName': userName,
      'password': password,
    });
    return TokenResponse.fromJson(data);
  }

  static Future<TokenResponse> refresh(String refreshToken) async {
    final data = await apiClient.post(ApiConfig.authRefresh, {
      'refreshToken': refreshToken,
    });
    return TokenResponse.fromJson(data);
  }

  static Future<void> logout(String refreshToken) async {
    await apiClient.post(ApiConfig.authLogout, {'refreshToken': refreshToken});
  }

  static Future<AppUser?> me() async {
    final data = await apiClient.get(ApiConfig.authMe);
    return data == null ? null : AppUser.fromJson(data);
  }

  static Future<void> changePassword(String current, String newPassword) async {
    await apiClient.post(ApiConfig.authChangePassword, {
      'currentPassword': current,
      'newPassword': newPassword,
    });
  }
}
