class Customer {
  final String id;
  final String name;
  final String? phone;
  final String? taxRegistrationNumber;
  final String? nationalId;
  final bool isCompany;
  final double balance;

  Customer({
    required this.id,
    required this.name,
    this.phone,
    this.taxRegistrationNumber,
    this.nationalId,
    required this.isCompany,
    required this.balance,
  });

  factory Customer.fromJson(Map<String, dynamic> json) => Customer(
        id: json['id'],
        name: json['name'] ?? '',
        phone: json['phone'],
        taxRegistrationNumber: json['taxRegistrationNumber'],
        nationalId: json['nationalId'],
        isCompany: json['isCompany'] ?? false,
        balance: (json['balance'] ?? 0).toDouble(),
      );
}
