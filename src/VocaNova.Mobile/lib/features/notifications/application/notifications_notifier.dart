import 'dart:async';

import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/features/notifications/application/notifications_state.dart';
import 'package:vocanova_mobile/features/notifications/data/notifications_repository.dart';
import 'package:vocanova_mobile/features/notifications/domain/app_notification.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

part 'notifications_notifier.g.dart';

@Riverpod(keepAlive: true)
NotificationsRepository notificationsRepository(Ref ref) =>
    NotificationsRepository(dio: DioClient.instance.dio);

// Lightweight unread count for the home-screen bell badge.
// Resilient by design: returns 0 (badge hidden) if the count can't be fetched
// — offline, not signed in yet, or the network layer isn't ready.
@riverpod
Future<int> notificationsUnreadCount(Ref ref) async {
  try {
    return await ref.read(notificationsRepositoryProvider).unreadCount();
  } catch (_) {
    return 0;
  }
}

@riverpod
class NotificationsNotifier extends _$NotificationsNotifier {
  @override
  NotificationsState build() => const NotificationsState();

  Future<void> load() async {
    state = state.copyWith(isLoading: true, clearError: true);
    try {
      final repo = ref.read(notificationsRepositoryProvider);
      final page = await repo.list(page: 1);
      final unread = await repo.unreadCount();
      state = NotificationsState(
        items: page.items,
        page: page.page,
        totalPages: page.totalPages,
        unreadCount: unread,
        isLoading: false,
      );
      ref.invalidate(notificationsUnreadCountProvider);
    } catch (_) {
      final l10n = lookupAppLocalizations(
        AppSettingsNotifier.instance.state.locale,
      );
      state = state.copyWith(
        isLoading: false,
        errorMessage: l10n.notifLoadError,
      );
    }
  }

  Future<void> loadMore() async {
    if (state.isLoadingMore || state.isLoading || !state.hasMore) {
      return;
    }
    state = state.copyWith(isLoadingMore: true);
    try {
      final next = await ref
          .read(notificationsRepositoryProvider)
          .list(page: state.page + 1);
      state = state.copyWith(
        items: [...state.items, ...next.items],
        page: next.page,
        totalPages: next.totalPages,
        isLoadingMore: false,
      );
    } catch (_) {
      state = state.copyWith(isLoadingMore: false);
    }
  }

  Future<void> markRead(int id) async {
    var found = false;
    final updated = <AppNotification>[];
    for (final n in state.items) {
      if (n.id == id && !n.isRead) {
        found = true;
        updated.add(n.copyWith(isRead: true, readAt: DateTime.now()));
      } else {
        updated.add(n);
      }
    }
    if (!found) {
      return;
    }
    state = state.copyWith(
      items: updated,
      unreadCount: state.unreadCount > 0 ? state.unreadCount - 1 : 0,
    );
    try {
      await ref.read(notificationsRepositoryProvider).markRead(id);
    } catch (_) {
      // Best-effort; a later refresh reconciles the read state.
    }
    ref.invalidate(notificationsUnreadCountProvider);
  }

  Future<void> markAllRead() async {
    if (state.unreadCount == 0) {
      return;
    }
    state = state.copyWith(
      items: [
        for (final n in state.items)
          n.isRead ? n : n.copyWith(isRead: true, readAt: DateTime.now()),
      ],
      unreadCount: 0,
    );
    try {
      await ref
          .read(notificationsRepositoryProvider)
          .markAllRead(state.items.map((n) => n.id));
    } catch (_) {
      // Best-effort.
    }
    ref.invalidate(notificationsUnreadCountProvider);
  }
}
