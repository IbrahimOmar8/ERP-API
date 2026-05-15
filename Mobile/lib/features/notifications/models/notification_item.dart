class NotificationItem {
  final String id;
  final String title;
  final String message;
  final String? type;
  final String? link;
  final String severity; // info | success | warning | error
  final bool isRead;
  final DateTime createdAt;

  NotificationItem({
    required this.id,
    required this.title,
    required this.message,
    this.type,
    this.link,
    required this.severity,
    required this.isRead,
    required this.createdAt,
  });

  factory NotificationItem.fromJson(Map<String, dynamic> j) => NotificationItem(
        id: j['id'] as String,
        title: j['title'] ?? '',
        message: j['message'] ?? '',
        type: j['type'],
        link: j['link'],
        severity: j['severity'] ?? 'info',
        isRead: j['isRead'] ?? false,
        createdAt: DateTime.parse(j['createdAt']),
      );
}
