import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/notifications/data/notifications_read_store.dart';
import 'package:vocanova_mobile/features/notifications/domain/app_notification.dart';

class NotificationsPage {
  const NotificationsPage({
    required this.items,
    required this.page,
    required this.totalPages,
  });

  final List<AppNotification> items;
  final int page;
  final int totalPages;
}

class NotificationsRepository {
  const NotificationsRepository({
    required Dio dio,
    NotificationsReadStore readStore = const NotificationsReadStore(),
  }) : _dio = dio,
       _readStore = readStore;

  final Dio _dio;
  final NotificationsReadStore _readStore;

  Future<NotificationsPage> list({required int page, int limit = 20}) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.notifications,
      queryParameters: {'page': page, 'limit': limit},
    );
    final data = response.data?['data'];
    if (data is! List) {
      throw const FormatException('Invalid notifications response.');
    }
    // The server can't know read state (notifications are derived), so overlay it
    // from the per-device read store.
    final readIds = await _readStore.readIds();
    final items = data
        .whereType<Map<String, dynamic>>()
        .map(AppNotification.fromJson)
        .map((n) => readIds.contains(n.id) ? n.copyWith(isRead: true) : n)
        .toList(growable: false);
    final pagination = response.data?['pagination'];
    final currentPage = pagination is Map<String, dynamic>
        ? (pagination['page'] as int? ?? page)
        : page;
    final totalPages = pagination is Map<String, dynamic>
        ? (pagination['totalPages'] as int? ?? 1)
        : 1;
    return NotificationsPage(
      items: items,
      page: currentPage,
      totalPages: totalPages,
    );
  }

  // Unread badge is computed locally from the newest notifications. A single page is
  // enough — "word deleted" notifications are rare, so they fit comfortably in one page.
  Future<int> unreadCount() async {
    final page = await list(page: 1, limit: 50);
    return page.items.where((n) => !n.isRead).length;
  }

  Future<void> markRead(int id) => _readStore.markRead(id);

  Future<void> markAllRead(Iterable<int> ids) => _readStore.markAllRead(ids);
}
