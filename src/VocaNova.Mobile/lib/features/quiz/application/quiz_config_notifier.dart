import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
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

  Future<void> loadTopics() async {
    try {
      final topics = await ref.read(wordSearchRepositoryProvider).getTopics();
      state = state.copyWith(topics: topics, isLoadingTopics: false);
    } catch (_) {
      state = state.copyWith(
        isLoadingTopics: false,
        errorMessage: 'Không thể tải chủ đề.',
      );
    }
  }

  void setListId(int? value) =>
      state = state.copyWith(listId: value, clearError: true);

  void setScope(String value) {
    state = state.copyWith(
      scopeType: value,
      clearDateFrom: value == 'all' || value == 'end_date',
      clearDateTo: value == 'all' || value == 'start_date',
      clearError: true,
    );
  }

  void setDateFrom(DateTime value) =>
      state = state.copyWith(dateFrom: value, clearError: true);

  void setDateTo(DateTime value) =>
      state = state.copyWith(dateTo: value, clearError: true);

  void toggleTopic(int id) {
    final ids = {...state.selectedTopicIds};
    ids.contains(id) ? ids.remove(id) : ids.add(id);
    state = state.copyWith(selectedTopicIds: ids, clearError: true);
  }

  void clearTopics() =>
      state = state.copyWith(selectedTopicIds: const {}, clearError: true);

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

  void setTimeLimit(int? value) =>
      state = state.copyWith(timeLimitSec: value, clearError: true);

  void setLives(int? value) =>
      state = state.copyWith(lives: value, clearError: true);

  String? validate() {
    if ((state.scopeType == 'start_date' || state.scopeType == 'date_range') &&
        state.dateFrom == null) {
      return 'Vui lòng chọn ngày bắt đầu.';
    }
    if ((state.scopeType == 'end_date' || state.scopeType == 'date_range') &&
        state.dateTo == null) {
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
      final result = await ref
          .read(quizRepositoryProvider)
          .createSession(
            QuizConfigRequest(
              mode: state.mode,
              questionType: state.questionType,
              scopeType: state.scopeType,
              listId: state.listId,
              scopeDateFrom: state.dateFrom,
              scopeDateTo: state.dateTo,
              topicIds: state.selectedTopicIds.toList(),
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
