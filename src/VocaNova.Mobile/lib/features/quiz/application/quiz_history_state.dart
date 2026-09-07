import 'package:vocanova_mobile/features/quiz/domain/models/quiz_history.dart';

class QuizHistoryState {
  const QuizHistoryState({
    this.items = const [],
    this.page = 0,
    this.totalPages = 1,
    this.isLoading = true,
    this.isLoadingMore = false,
    this.errorMessage,
  });

  final List<QuizHistoryItem> items;
  final int page;
  final int totalPages;
  final bool isLoading;
  final bool isLoadingMore;
  final String? errorMessage;

  bool get hasMore => page < totalPages;

  QuizHistoryState copyWith({
    List<QuizHistoryItem>? items,
    int? page,
    int? totalPages,
    bool? isLoading,
    bool? isLoadingMore,
    String? errorMessage,
    bool clearError = false,
  }) => QuizHistoryState(
    items: items ?? this.items,
    page: page ?? this.page,
    totalPages: totalPages ?? this.totalPages,
    isLoading: isLoading ?? this.isLoading,
    isLoadingMore: isLoadingMore ?? this.isLoadingMore,
    errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
  );
}
