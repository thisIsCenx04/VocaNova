import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/features/notifications/application/notifications_notifier.dart';
import 'package:vocanova_mobile/features/notifications/data/services/notifications_api_service.dart';
import 'package:vocanova_mobile/features/notifications/domain/models/app_notification.dart';

void main() {
  test('loading notifications refreshes the home unread count', () async {
    final repository = FakeNotificationsApiService();
    final container = ProviderContainer(
      overrides: [
        notificationsApiServiceProvider.overrideWithValue(repository),
      ],
    );
    addTearDown(container.dispose);

    expect(await container.read(notificationsUnreadCountProvider.future), 0);

    repository.items = [unreadNotification];
    await container.read(notificationsProvider.notifier).load();

    expect(await container.read(notificationsUnreadCountProvider.future), 1);
  });
}

final unreadNotification = AppNotification(
  id: 7,
  type: 'word_deleted',
  title: 'Từ vựng đã bị gỡ',
  message: 'Từ "hello" đã bị gỡ khỏi từ điển.',
  refType: 'word',
  refId: 7,
  isRead: false,
  createdAt: DateTime.utc(2026, 8, 3),
);

class FakeNotificationsApiService extends NotificationsApiService {
  FakeNotificationsApiService() : super(dio: Dio());

  List<AppNotification> items = [];

  @override
  Future<NotificationsPage> list({required int page, int limit = 20}) async =>
      NotificationsPage(items: items, page: page, totalPages: 1);

  @override
  Future<int> unreadCount() async =>
      items.where((notification) => !notification.isRead).length;
}
