class Supplier {
  final String id;
  final String name;
  final String? phone;
  final String? email;
  final String? address;
  final String? taxRegistrationNumber;
  final double balance;
  final bool isActive;

  Supplier({
    required this.id,
    required this.name,
    this.phone,
    this.email,
    this.address,
    this.taxRegistrationNumber,
    required this.balance,
    required this.isActive,
  });

  factory Supplier.fromJson(Map<String, dynamic> j) => Supplier(
        id: j['id'],
        name: j['name'] ?? '',
        phone: j['phone'],
        email: j['email'],
        address: j['address'],
        taxRegistrationNumber: j['taxRegistrationNumber'],
        balance: (j['balance'] ?? 0).toDouble(),
        isActive: j['isActive'] ?? true,
      );
}
