import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/loading_shimmer.dart';
import '../models/notification_item.dart';
import '../services/notification_service.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  List<NotificationItem> _items = [];
  bool _loading = true;
  bool _unreadOnly = false;

  static final _df = DateFormat('yyyy-MM-dd HH:mm');

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      _items = await NotificationService.getMine(unreadOnly: _unreadOnly);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _markRead(NotificationItem n) async {
    if (n.isRead) return;
    try {
      await NotificationService.markRead(n.id);
      _load();
    } catch (_) {/* silent */}
  }

  Future<void> _markAllRead() async {
    try {
      await NotificationService.markAllRead();
      _load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$e')));
      }
    }
  }

  Color _severityColor(String severity) {
    switch (severity) {
      case 'success': return Colors.green;
      case 'warning': return Colors.amber.shade700;
      case 'error':   return Colors.red;
      default:        return Colors.blue;
    }
  }

  IconData _severityIcon(String severity) {
    switch (severity) {
      case 'success': return Icons.check_circle;
      case 'warning': return Icons.warning_amber;
      case 'error':   return Icons.error_outline;
      default:        return Icons.info_outline;
    }
  }

  @override
  Widget build(BuildContext context) {
    final unreadCount = _items.where((n) => !n.isRead).length;
    return Scaffold(
      appBar: AppBar(
        title: const Text('الإشعارات'),
        actions: [
          if (unreadCount > 0)
            IconButton(
              icon: const Icon(Icons.done_all),
              tooltip: 'قراءة الكل',
              onPressed: _markAllRead,
            ),
          IconButton(icon: const Icon(Icons.refresh), onPressed: _load),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              children: [
                const Text('غير المقروءة فقط'),
                const Spacer(),
                Switch(
                  value: _unreadOnly,
                  onChanged: (v) {
                    setState(() => _unreadOnly = v);
                    _load();
                  },
                ),
              ],
            ),
          ),
          Expanded(
            child: _loading
                ? const LoadingShimmerList(itemHeight: 78)
                : _items.isEmpty
                    ? EmptyState(
                        icon: Icons.notifications_none,
                        message: _unreadOnly ? 'لا توجد إشعارات غير مقروءة' : 'لا توجد إشعارات',
                      )
                    : RefreshIndicator(
                        onRefresh: _load,
                        child: ListView.separated(
                          padding: const EdgeInsets.fromLTRB(12, 0, 12, 24),
                          itemCount: _items.length,
                          separatorBuilder: (_, __) => const SizedBox(height: 6),
                          itemBuilder: (_, i) {
                            final n = _items[i];
                            final color = _severityColor(n.severity);
                            return Card(
                              child: InkWell(
                                borderRadius: BorderRadius.circular(12),
                                onTap: () => _markRead(n),
                                child: Container(
                                  decoration: BoxDecoration(
                                    border: Border(
                                      right: BorderSide(color: color, width: 4),
                                    ),
                                    color: n.isRead
                                        ? null
                                        : color.withOpacity(0.04),
                                  ),
                                  padding: const EdgeInsets.all(12),
                                  child: Row(
                                    children: [
                                      Icon(_severityIcon(n.severity), color: color, size: 22),
                                      const SizedBox(width: 10),
                                      Expanded(
                                        child: Column(
                                          crossAxisAlignment: CrossAxisAlignment.start,
                                          children: [
                                            Row(
                                              children: [
                                                Expanded(
                                                  child: Text(
                                                    n.title,
                                                    style: TextStyle(
                                                      fontWeight: n.isRead
                                                          ? FontWeight.w500
                                                          : FontWeight.bold,
                                                    ),
                                                  ),
                                                ),
                                                if (!n.isRead)
                                                  Container(
                                                    width: 8,
                                                    height: 8,
                                                    decoration: BoxDecoration(
                                                      color: color,
                                                      shape: BoxShape.circle,
                                                    ),
                                                  ),
                                              ],
                                            ),
                                            const SizedBox(height: 4),
                                            Text(n.message,
                                                style: const TextStyle(fontSize: 12.5, color: Colors.grey)),
                                            const SizedBox(height: 4),
                                            Text(_df.format(n.createdAt.toLocal()),
                                                style: const TextStyle(fontSize: 11, color: Colors.grey)),
                                          ],
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                            );
                          },
                        ),
                      ),
          ),
        ],
      ),
    );
  }
}
