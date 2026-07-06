import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
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
  const NotificationsRepository({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<NotificationsPage> list({
    required int page,
    int limit = 20,
    bool unreadOnly = false,
  }) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.notifications,
      queryParameters: {
        'page': page,
        'limit': limit,
        if (unreadOnly) 'unreadOnly': true,
      },
    );
    final data = response.data?['data'];
    if (data is! List) {
      throw const FormatException('Invalid notifications response.');
    }
    final items = data
        .whereType<Map<String, dynamic>>()
        .map(AppNotification.fromJson)
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

  Future<int> unreadCount() async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.notificationsUnreadCount,
    );
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      return 0;
    }
    return (data['count'] as num?)?.toInt() ?? 0;
  }

  Future<void> markRead(int id) async {
    await _dio.patch<Map<String, dynamic>>(ApiEndpoints.notificationRead(id));
  }

  Future<int> markAllRead() async {
    final response = await _dio.patch<Map<String, dynamic>>(
      ApiEndpoints.notificationsReadAll,
    );
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      return 0;
    }
    return (data['updated'] as num?)?.toInt() ?? 0;
  }
}
