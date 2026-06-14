import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';

class WordSearchState {
  const WordSearchState({
    this.query = '',
    this.results = const [],
    this.history = const [],
    this.topics = const [],
    this.selectedCefr,
    this.selectedTopicId,
    this.isLoading = false,
    this.isOffline = false,
    this.errorMessage,
  });

  final String query;
  final List<WordSummary> results;
  final List<String> history;
  final List<TopicSummary> topics;
  final String? selectedCefr;
  final int? selectedTopicId;
  final bool isLoading;
  final bool isOffline;
  final String? errorMessage;

  WordSearchState copyWith({
    String? query,
    List<WordSummary>? results,
    List<String>? history,
    List<TopicSummary>? topics,
    String? selectedCefr,
    int? selectedTopicId,
    bool? isLoading,
    bool? isOffline,
    String? errorMessage,
    bool clearCefr = false,
    bool clearTopic = false,
    bool clearError = false,
  }) {
    return WordSearchState(
      query: query ?? this.query,
      results: results ?? this.results,
      history: history ?? this.history,
      topics: topics ?? this.topics,
      selectedCefr: clearCefr ? null : selectedCefr ?? this.selectedCefr,
      selectedTopicId: clearTopic
          ? null
          : selectedTopicId ?? this.selectedTopicId,
      isLoading: isLoading ?? this.isLoading,
      isOffline: isOffline ?? this.isOffline,
      errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
    );
  }
}
