import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/features/notifications/application/notifications_notifier.dart';
import 'package:vocanova_mobile/features/notifications/application/notifications_state.dart';
import 'package:vocanova_mobile/features/notifications/domain/models/app_notification.dart';
import 'package:vocanova_mobile/features/notifications/presentation/notifications_screen.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

void main() {
  testWidgets(
    'deleted word notification shows its message without opening word detail',
    (tester) async {
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            notificationsProvider.overrideWith(FakeNotificationsNotifier.new),
          ],
          child: const MaterialApp(
            locale: Locale('vi'),
            localizationsDelegates: AppLocalizations.localizationsDelegates,
            supportedLocales: AppLocalizations.supportedLocales,
            home: NotificationsScreen(),
          ),
        ),
      );
      await tester.pump();

      await tester.tap(find.text(deletedWordNotification.title));
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('word-deleted-notification-dialog')),
        findsOneWidget,
      );
      expect(find.text(deletedWordNotification.message), findsWidgets);
      expect(
        find.byKey(const Key('notification-dialog-close')),
        findsOneWidget,
      );
      expect(tester.takeException(), isNull);
    },
  );
}

final deletedWordNotification = AppNotification(
  id: 7,
  type: 'word_deleted',
  title: 'Từ vựng đã bị gỡ',
  message: 'Từ "hello" đã bị gỡ khỏi từ điển.',
  refType: 'word',
  refId: 7,
  isRead: false,
  createdAt: DateTime.utc(2026, 8, 3),
);

class FakeNotificationsNotifier extends NotificationsNotifier {
  @override
  NotificationsState build() => NotificationsState(
    items: [deletedWordNotification],
    page: 1,
    totalPages: 1,
    unreadCount: 1,
    isLoading: false,
  );

  @override
  Future<void> load() async {}

  @override
  Future<void> markRead(int id) async {
    state = state.copyWith(
      items: [
        for (final notification in state.items)
          notification.id == id
              ? notification.copyWith(isRead: true)
              : notification,
      ],
      unreadCount: 0,
    );
  }
}
