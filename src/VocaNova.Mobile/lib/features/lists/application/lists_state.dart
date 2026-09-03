import 'package:vocanova_mobile/features/dictionary/domain/models/word_summary.dart';
import 'package:vocanova_mobile/features/lists/domain/models/user_list.dart';

class ListsState {
  const ListsState({
    this.lists = const [],
    this.personalTopics = const [],
    this.isLoading = true,
    this.isOffline = false,
    this.isMutating = false,
    this.errorMessage,
  });

  final List<UserList> lists;
  final List<PersonalTopicSummary> personalTopics;
  final bool isLoading;
  final bool isOffline;
  final bool isMutating;
  final String? errorMessage;

  ListsState copyWith({
    List<UserList>? lists,
    List<PersonalTopicSummary>? personalTopics,
    bool? isLoading,
    bool? isOffline,
    bool? isMutating,
    String? errorMessage,
    bool clearError = false,
  }) => ListsState(
    lists: lists ?? this.lists,
    personalTopics: personalTopics ?? this.personalTopics,
    isLoading: isLoading ?? this.isLoading,
    isOffline: isOffline ?? this.isOffline,
    isMutating: isMutating ?? this.isMutating,
    errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
  );
}
