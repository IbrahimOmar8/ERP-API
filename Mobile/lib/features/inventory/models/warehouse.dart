class Warehouse {
  final String id;
  final String nameAr;
  final String? nameEn;
  final String code;
  final bool isMain;
  final bool isActive;

  Warehouse({
    required this.id,
    required this.nameAr,
    this.nameEn,
    required this.code,
    required this.isMain,
    required this.isActive,
  });

  factory Warehouse.fromJson(Map<String, dynamic> json) => Warehouse(
        id: json['id'],
        nameAr: json['nameAr'] ?? '',
        nameEn: json['nameEn'],
        code: json['code'] ?? '',
        isMain: json['isMain'] ?? false,
        isActive: json['isActive'] ?? true,
      );
}
