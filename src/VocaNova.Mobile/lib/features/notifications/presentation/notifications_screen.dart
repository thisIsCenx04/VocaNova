import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/features/notifications/application/notifications_notifier.dart';
import 'package:vocanova_mobile/features/notifications/application/notifications_state.dart';
import 'package:vocanova_mobile/features/notifications/domain/app_notification.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class NotificationsScreen extends ConsumerStatefulWidget {
  const NotificationsScreen({super.key});

  @override
  ConsumerState<NotificationsScreen> createState() =>
      _NotificationsScreenState();
}

class _NotificationsScreenState extends ConsumerState<NotificationsScreen> {
  final ScrollController _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(notificationsProvider.notifier).load();
    });
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollController.position.pixels >=
        _scrollController.position.maxScrollExtent - 200) {
      ref.read(notificationsProvider.notifier).loadMore();
    }
  }

  Future<void> _open(AppNotification notification) async {
    await ref
        .read(notificationsProvider.notifier)
        .markRead(notification.id);
    if (!mounted) {
      return;
    }
    if (notification.refType == 'word' && notification.refId != null) {
      context.push(AppRoutes.wordDetail(notification.refId.toString()));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final state = ref.watch(notificationsProvider);
    final notifier = ref.read(notificationsProvider.notifier);
    final dark = Theme.of(context).brightness == Brightness.dark;
    final background = dark ? AppColors.background : AppColors.lightBackground;

    return Scaffold(
      backgroundColor: background,
      appBar: AppBar(
        title: Text(l10n.notifTitle),
        actions: [
          if (state.unreadCount > 0)
            TextButton(
              onPressed: notifier.markAllRead,
              child: Text(l10n.notifMarkAllRead),
            ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: notifier.load,
        child: _buildBody(context, state, dark),
      ),
    );
  }

  Widget _buildBody(BuildContext context, NotificationsState state, bool dark) {
    if (state.isLoading && state.items.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.items.isEmpty) {
      final l10n = AppLocalizations.of(context)!;
      return _EmptyOrError(
        message: state.errorMessage ?? l10n.notifEmptyMessage,
        isError: state.errorMessage != null,
        onRetry: () => ref.read(notificationsProvider.notifier).load(),
      );
    }

    final secondary = dark ? const Color(0xFFB8B3C3) : const Color(0xFF888888);
    return ListView.separated(
      controller: _scrollController,
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.symmetric(vertical: 8),
      itemCount: state.items.length + (state.isLoadingMore ? 1 : 0),
      separatorBuilder: (_, _) =>
          Divider(height: 1, color: secondary.withValues(alpha: 0.15)),
      itemBuilder: (context, index) {
        if (index >= state.items.length) {
          return const Padding(
            padding: EdgeInsets.all(16),
            child: Center(child: CircularProgressIndicator()),
          );
        }
        return _NotificationTile(
          notification: state.items[index],
          dark: dark,
          onTap: () => _open(state.items[index]),
        );
      },
    );
  }
}

class _NotificationTile extends StatelessWidget {
  const _NotificationTile({
    required this.notification,
    required this.dark,
    required this.onTap,
  });

  final AppNotification notification;
  final bool dark;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final text = dark ? AppColors.onSurface : Colors.black;
    final secondary = dark ? const Color(0xFFB8B3C3) : const Color(0xFF888888);
    final unreadTint = AppColors.primary.withValues(alpha: dark ? 0.10 : 0.08);

    return Material(
      color: notification.isRead ? Colors.transparent : unreadTint,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 14, 16, 14),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 38,
                height: 38,
                decoration: BoxDecoration(
                  color: AppColors.primary.withValues(alpha: 0.16),
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.notifications_outlined,
                  size: 19,
                  color: AppColors.primary,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      notification.title,
                      style: TextStyle(
                        color: text,
                        fontSize: 14,
                        fontWeight: notification.isRead
                            ? FontWeight.w600
                            : FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      notification.message,
                      style: TextStyle(
                        color: secondary,
                        fontSize: 13,
                        height: 1.4,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      _relativeTime(l10n, notification.createdAt),
                      style: TextStyle(
                        color: secondary.withValues(alpha: 0.8),
                        fontSize: 11,
                      ),
                    ),
                  ],
                ),
              ),
              if (!notification.isRead)
                Container(
                  margin: const EdgeInsets.only(left: 8, top: 4),
                  width: 8,
                  height: 8,
                  decoration: const BoxDecoration(
                    color: AppColors.primary,
                    shape: BoxShape.circle,
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  static String _relativeTime(AppLocalizations l10n, DateTime dateTime) {
    final diff = DateTime.now().difference(dateTime.toLocal());
    if (diff.isNegative || diff.inMinutes < 1) {
      return l10n.notifJustNow;
    }
    if (diff.inMinutes < 60) {
      return l10n.notifMinutesAgo(diff.inMinutes);
    }
    if (diff.inHours < 24) {
      return l10n.notifHoursAgo(diff.inHours);
    }
    if (diff.inDays < 30) {
      return l10n.notifDaysAgo(diff.inDays);
    }
    final d = dateTime.toLocal();
    return '${d.day}/${d.month}/${d.year}';
  }
}

class _EmptyOrError extends StatelessWidget {
  const _EmptyOrError({
    required this.message,
    required this.isError,
    required this.onRetry,
  });

  final String message;
  final bool isError;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final dark = Theme.of(context).brightness == Brightness.dark;
    final secondary = dark ? const Color(0xFFB8B3C3) : const Color(0xFF888888);
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      children: [
        SizedBox(height: MediaQuery.of(context).size.height * 0.25),
        Icon(
          isError ? Icons.error_outline : Icons.notifications_off_outlined,
          size: 44,
          color: secondary,
        ),
        const SizedBox(height: 12),
        Text(
          message,
          textAlign: TextAlign.center,
          style: TextStyle(color: secondary, fontSize: 14),
        ),
        if (isError) ...[
          const SizedBox(height: 12),
          Center(
            child: TextButton(
              onPressed: onRetry,
              child: Text(AppLocalizations.of(context)!.notifRetry),
            ),
          ),
        ],
      ],
    );
  }
}
