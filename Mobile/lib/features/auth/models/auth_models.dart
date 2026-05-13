class AppUser {
  final String id;
  final String userName;
  final String fullName;
  final String? email;
  final String? phone;
  final String? defaultWarehouseId;
  final String? defaultCashRegisterId;
  final bool isActive;
  final List<String> roles;

  AppUser({
    required this.id,
    required this.userName,
    required this.fullName,
    this.email,
    this.phone,
    this.defaultWarehouseId,
    this.defaultCashRegisterId,
    required this.isActive,
    required this.roles,
  });

  factory AppUser.fromJson(Map<String, dynamic> json) => AppUser(
        id: json['id'],
        userName: json['userName'] ?? '',
        fullName: json['fullName'] ?? '',
        email: json['email'],
        phone: json['phone'],
        defaultWarehouseId: json['defaultWarehouseId'],
        defaultCashRegisterId: json['defaultCashRegisterId'],
        isActive: json['isActive'] ?? true,
        roles: ((json['roles'] ?? []) as List).cast<String>(),
      );

  bool hasRole(String role) => roles.contains(role);
  bool get isAdmin => hasRole('Admin');
  bool get isCashier => hasRole('Cashier');
  bool get isWarehouseKeeper => hasRole('WarehouseKeeper');
}

class TokenResponse {
  final String accessToken;
  final String refreshToken;
  final DateTime expiresAt;
  final AppUser user;

  TokenResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresAt,
    required this.user,
  });

  factory TokenResponse.fromJson(Map<String, dynamic> json) => TokenResponse(
        accessToken: json['accessToken'],
        refreshToken: json['refreshToken'],
        expiresAt: DateTime.parse(json['expiresAt']),
        user: AppUser.fromJson(json['user']),
      );
}
