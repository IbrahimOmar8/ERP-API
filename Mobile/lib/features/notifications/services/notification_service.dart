import '../../../core/api/api_client.dart';
import '../models/notification_item.dart';

class NotificationService {
  static const _path = '/Notifications';

  static Future<List<NotificationItem>> getMine({bool unreadOnly = false, int take = 30}) async {
    final data = await apiClient.get(_path, query: {
      'unreadOnly': unreadOnly,
      'take': take,
    });
    return (data as List).map((e) => NotificationItem.fromJson(e)).toList();
  }

  static Future<int> getUnreadCount() async {
    final data = await apiClient.get('$_path/unread-count');
    return (data['count'] ?? 0) as int;
  }

  static Future<void> markRead(String id) async {
    await apiClient.post('$_path/$id/read', {});
  }

  static Future<int> markAllRead() async {
    final data = await apiClient.post('$_path/read-all', {});
    return (data?['marked'] ?? 0) as int;
  }
}
