import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/domain/user_list.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_state.dart';
import 'package:vocanova_mobile/features/quiz/data/quiz_repository.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_config.dart';

part 'quiz_config_notifier.g.dart';

@Riverpod(keepAlive: true)
QuizRepository quizRepository(Ref ref) =>
    QuizRepository(dio: DioClient.instance.dio);

@riverpod
class QuizConfigNotifier extends _$QuizConfigNotifier {
  @override
  QuizConfigState build() => const QuizConfigState();

  Future<void> loadSources() async {
    try {
      final listsFuture = ref.read(listsRepositoryProvider).getLists();
      final topicsFuture = ref
          .read(wordSearchRepositoryProvider)
          .getPersonalTopics();
      final sources = await Future.wait<Object>([listsFuture, topicsFuture]);
      final lists = sources[0] as List<UserList>;
      final personalTopics = sources[1] as List<PersonalTopicSummary>;
      var sourceType = state.sourceType;
      final selectedListId = state.listId;
      if (selectedListId != null &&
          personalTopics.any((topic) => topic.listId == selectedListId)) {
        sourceType = QuizSourceType.personalTopic;
      } else if (selectedListId != null &&
          lists.any((list) => list.listId == selectedListId)) {
        sourceType = QuizSourceType.myList;
      }
      state = state.copyWith(
        lists: lists,
        personalTopics: personalTopics,
        sourceType: sourceType,
        isLoadingSources: false,
      );
    } catch (_) {
      state = state.copyWith(
        isLoadingSources: false,
        errorMessage: 'Không thể tải nguồn kiểm tra.',
      );
    }
  }

  void setListId(int? value) => state = state.copyWith(
    listId: value,
    clearListId: value == null,
    clearError: true,
  );

  void setSourceType(String value) {
    if (value == state.sourceType) return;
    state = state.copyWith(
      sourceType: value,
      clearListId: true,
      clearError: true,
    );
  }

  void selectSource(int listId) =>
      state = state.copyWith(listId: listId, clearError: true);

  void setScope(String value) {
    state = state.copyWith(
      scopeType: value,
      clearDateFrom: value != 'date_range',
      clearDateTo: value != 'date_range',
      clearError: true,
    );
  }

  void setDateFrom(DateTime value) =>
      state = state.copyWith(dateFrom: value, clearError: true);

  void setDateTo(DateTime value) =>
      state = state.copyWith(dateTo: value, clearError: true);

  void setMode(String value) {
    state = state.copyWith(
      mode: value,
      clearTime: value != 'timed',
      clearLives: value != 'elimination',
      clearError: true,
    );
  }

  void setQuestionType(int value) =>
      state = state.copyWith(questionType: value, clearError: true);

  void setAnswerMethod(String value) =>
      state = state.copyWith(answerMethod: value, clearError: true);

  void setWordOrder(String value) =>
      state = state.copyWith(wordOrder: value, clearError: true);

  void setQuestionLimit(int? value) => state = state.copyWith(
    questionLimit: value,
    clearQuestionLimit: value == null,
    clearError: true,
  );

  void setTimeLimit(int? value) =>
      state = state.copyWith(timeLimitSec: value, clearError: true);

  void setLives(int? value) =>
      state = state.copyWith(lives: value, clearError: true);

  String? validate() {
    if (state.listId == null) {
      return 'Vui lòng chọn danh sách hoặc chủ đề cá nhân để kiểm tra.';
    }
    if ((state.scopeType == 'start_date' || state.scopeType == 'date_range') &&
        state.dateFrom == null) {
      return 'Vui lòng chọn ngày bắt đầu.';
    }
    if (state.scopeType == 'date_range' && state.dateTo == null) {
      return 'Vui lòng chọn ngày kết thúc.';
    }
    if (state.dateFrom != null &&
        state.dateTo != null &&
        state.dateFrom!.isAfter(state.dateTo!)) {
      return 'Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.';
    }
    if (state.mode == 'timed' && (state.timeLimitSec ?? 0) <= 0) {
      return 'Chế độ tính giờ cần thời gian lớn hơn 0.';
    }
    if (state.mode == 'elimination' && (state.lives ?? 0) <= 0) {
      return 'Chế độ loại trực tiếp cần số mạng lớn hơn 0.';
    }
    return null;
  }

  Future<QuizSessionStart?> createSession() async {
    final error = validate();
    if (error != null) {
      state = state.copyWith(errorMessage: error);
      return null;
    }
    state = state.copyWith(isCreating: true, clearError: true);
    try {
      var scopeType = state.scopeType;
      var dateFrom = state.dateFrom;
      var dateTo = state.dateTo;
      if (scopeType == 'this_week') {
        // Backend không có scope tuần; quy về start_date từ thứ Hai tuần này.
        final today = DateTime.now();
        scopeType = 'start_date';
        dateFrom = DateTime(
          today.year,
          today.month,
          today.day,
        ).subtract(Duration(days: today.weekday - 1));
        dateTo = null;
      }
      final result = await ref
          .read(quizRepositoryProvider)
          .createSession(
            QuizConfigRequest(
              mode: state.mode,
              questionType: state.questionType,
              scopeType: scopeType,
              wordOrder: state.wordOrder,
              wordLimit: state.questionLimit,
              listId: state.listId,
              scopeDateFrom: state.dateFrom,
              scopeDateTo: state.dateTo,
              topicIds: const [],
              timeLimitSec: state.timeLimitSec,
              lives: state.lives,
              answerMethod: state.answerMethod,
            ),
          );
      state = state.copyWith(isCreating: false);
      return result;
    } catch (_) {
      state = state.copyWith(
        isCreating: false,
        errorMessage: 'Không thể tạo bài kiểm tra. Hãy kiểm tra số lượng từ.',
      );
      return null;
    }
  }
}
