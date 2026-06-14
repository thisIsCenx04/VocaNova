import 'package:vocanova_mobile/features/lists/domain/list_word.dart';

class ListDetailState {
  const ListDetailState({
    this.words = const [],
    this.page = 0,
    this.totalPages = 1,
    this.isLoading = true,
    this.isLoadingMore = false,
    this.isOffline = false,
    this.errorMessage,
  });

  final List<ListWord> words;
  final int page;
  final int totalPages;
  final bool isLoading;
  final bool isLoadingMore;
  final bool isOffline;
  final String? errorMessage;

  bool get hasMore => page < totalPages;

  ListDetailState copyWith({
    List<ListWord>? words,
    int? page,
    int? totalPages,
    bool? isLoading,
    bool? isLoadingMore,
    bool? isOffline,
    String? errorMessage,
    bool clearError = false,
  }) => ListDetailState(
    words: words ?? this.words,
    page: page ?? this.page,
    totalPages: totalPages ?? this.totalPages,
    isLoading: isLoading ?? this.isLoading,
    isLoadingMore: isLoadingMore ?? this.isLoadingMore,
    isOffline: isOffline ?? this.isOffline,
    errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
  );
}
