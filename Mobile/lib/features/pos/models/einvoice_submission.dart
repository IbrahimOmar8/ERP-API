class EInvoiceSubmission {
  final String id;
  final String saleId;
  final String? submissionUuid;
  final String? longId;
  final String? hashKey;
  final int status;
  final String? errorMessage;
  final DateTime createdAt;
  final DateTime? submittedAt;
  final DateTime? validatedAt;

  EInvoiceSubmission({
    required this.id,
    required this.saleId,
    this.submissionUuid,
    this.longId,
    this.hashKey,
    required this.status,
    this.errorMessage,
    required this.createdAt,
    this.submittedAt,
    this.validatedAt,
  });

  factory EInvoiceSubmission.fromJson(Map<String, dynamic> json) =>
      EInvoiceSubmission(
        id: json['id'],
        saleId: json['saleId'],
        submissionUuid: json['submissionUuid'],
        longId: json['longId'],
        hashKey: json['hashKey'],
        status: json['status'] ?? 0,
        errorMessage: json['errorMessage'],
        createdAt: DateTime.parse(json['createdAt']),
        submittedAt: json['submittedAt'] == null
            ? null
            : DateTime.parse(json['submittedAt']),
        validatedAt: json['validatedAt'] == null
            ? null
            : DateTime.parse(json['validatedAt']),
      );

  // Matches Domain.Enums.EInvoiceStatus
  String get statusLabel => switch (status) {
        0 => 'قيد الإرسال',
        1 => 'تم التقديم',
        2 => 'مقبولة',
        3 => 'غير صالحة',
        4 => 'مرفوضة',
        5 => 'ملغاة',
        _ => 'غير معروف',
      };
}
