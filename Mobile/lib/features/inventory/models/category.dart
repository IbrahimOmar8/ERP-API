class Category {
  final String id;
  final String nameAr;
  final String? nameEn;
  final String? parentCategoryId;
  final bool isActive;

  Category({
    required this.id,
    required this.nameAr,
    this.nameEn,
    this.parentCategoryId,
    required this.isActive,
  });

  factory Category.fromJson(Map<String, dynamic> json) => Category(
        id: json['id'],
        nameAr: json['nameAr'] ?? '',
        nameEn: json['nameEn'],
        parentCategoryId: json['parentCategoryId'],
        isActive: json['isActive'] ?? true,
      );
}
