import 'package:vocanova_mobile/features/notifications/domain/app_notification.dart';

class NotificationsState {
  const NotificationsState({
    this.items = const [],
    this.page = 0,
    this.totalPages = 1,
    this.unreadCount = 0,
    this.isLoading = true,
    this.isLoadingMore = false,
    this.errorMessage,
  });

  final List<AppNotification> items;
  final int page;
  final int totalPages;
  final int unreadCount;
  final bool isLoading;
  final bool isLoadingMore;
  final String? errorMessage;

  bool get hasMore => page < totalPages;

  NotificationsState copyWith({
    List<AppNotification>? items,
    int? page,
    int? totalPages,
    int? unreadCount,
    bool? isLoading,
    bool? isLoadingMore,
    String? errorMessage,
    bool clearError = false,
  }) => NotificationsState(
    items: items ?? this.items,
    page: page ?? this.page,
    totalPages: totalPages ?? this.totalPages,
    unreadCount: unreadCount ?? this.unreadCount,
    isLoading: isLoading ?? this.isLoading,
    isLoadingMore: isLoadingMore ?? this.isLoadingMore,
    errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
  );
}
