import 'package:vocanova_mobile/features/dictionary/domain/models/word_detail.dart';

class WordDetailState {
  const WordDetailState({
    this.word,
    this.isLoading = true,
    this.isOffline = false,
    this.isSaved = false,
    this.errorMessage,
  });

  final WordDetail? word;
  final bool isLoading;
  final bool isOffline;
  final bool isSaved;
  final String? errorMessage;

  WordDetailState copyWith({
    WordDetail? word,
    bool? isLoading,
    bool? isOffline,
    bool? isSaved,
    String? errorMessage,
    bool clearError = false,
  }) => WordDetailState(
    word: word ?? this.word,
    isLoading: isLoading ?? this.isLoading,
    isOffline: isOffline ?? this.isOffline,
    isSaved: isSaved ?? this.isSaved,
    errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
  );
}
