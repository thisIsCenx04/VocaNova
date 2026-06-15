import 'package:vocanova_mobile/features/quiz/domain/quiz_result.dart';

class WrongWordsState {
  const WrongWordsState({
    this.words = const [],
    this.page = 0,
    this.totalPages = 1,
    this.isLoading = true,
    this.isLoadingMore = false,
    this.errorMessage,
  });

  final List<WrongWord> words;
  final int page;
  final int totalPages;
  final bool isLoading;
  final bool isLoadingMore;
  final String? errorMessage;

  bool get hasMore => page < totalPages;

  WrongWordsState copyWith({
    List<WrongWord>? words,
    int? page,
    int? totalPages,
    bool? isLoading,
    bool? isLoadingMore,
    String? errorMessage,
    bool clearError = false,
  }) => WrongWordsState(
    words: words ?? this.words,
    page: page ?? this.page,
    totalPages: totalPages ?? this.totalPages,
    isLoading: isLoading ?? this.isLoading,
    isLoadingMore: isLoadingMore ?? this.isLoadingMore,
    errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
  );
}
