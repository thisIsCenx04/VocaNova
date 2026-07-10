import 'dart:async';

import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_session_state.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_config.dart';

part 'quiz_session_notifier.g.dart';

@riverpod
class QuizSessionNotifier extends _$QuizSessionNotifier {
  Timer? _timer;

  @override
  QuizSessionState build(QuizSessionStart session) {
    ref.onDispose(() => _timer?.cancel());
    if (session.mode == 'timed' && session.timeLimitSec != null) {
      _timer = Timer.periodic(const Duration(seconds: 1), (_) => _tick());
    }
    return QuizSessionState(
      question: session.firstQuestion,
      questionNumber: 1,
      questionCount: session.questionCount,
      remainingLives: session.mode == 'elimination' ? session.lives : null,
      remainingSeconds: session.mode == 'timed' ? session.timeLimitSec : null,
    );
  }

  Future<void> submitAnswer(String answer) async {
    if (state.isSubmitting || state.hasAnswered || state.isFinished) return;
    state = state.copyWith(
      selectedAnswer: answer,
      isSubmitting: true,
      clearError: true,
    );
    try {
      final result = await ref
          .read(quizRepositoryProvider)
          .submitAnswer(
            sessionId: session.sessionId,
            wordId: state.question.wordId,
            answer: answer,
            listId: session.listId,
          );
      final lives = state.remainingLives == null || result.isCorrect
          ? state.remainingLives
          : state.remainingLives! - 1;
      final naturallyFinished = result.nextQuestion == null;
      final exhaustedLives = lives != null && lives <= 0;
      state = state.copyWith(
        answerResult: result,
        remainingLives: lives,
        isSubmitting: false,
        isFinished: naturallyFinished,
      );
      if (naturallyFinished) _timer?.cancel();
      if (exhaustedLives && result.nextQuestion != null) {
        await finish();
      }
    } catch (_) {
      state = state.copyWith(
        isSubmitting: false,
        errorMessage: 'Không thể gửi câu trả lời. Vui lòng thử lại.',
      );
    }
  }

  void nextQuestion() {
    final next = state.answerResult?.nextQuestion;
    if (next == null || state.isFinished) return;
    state = state.copyWith(
      question: next,
      questionNumber: state.questionNumber + 1,
      clearAnswer: true,
      clearError: true,
    );
  }

  Future<bool> finish() async {
    if (state.isFinishing) return false;
    _timer?.cancel();
    state = state.copyWith(isFinishing: true, clearError: true);
    try {
      await ref.read(quizRepositoryProvider).finishSession(session.sessionId);
      state = state.copyWith(isFinishing: false, isFinished: true);
      return true;
    } catch (_) {
      state = state.copyWith(
        isFinishing: false,
        errorMessage: 'Không thể kết thúc bài kiểm tra. Vui lòng thử lại.',
      );
      return false;
    }
  }

  void _tick() {
    if (state.isFinished || state.isFinishing) return;
    final seconds = (state.remainingSeconds ?? 0) - 1;
    if (seconds <= 0) {
      state = state.copyWith(remainingSeconds: 0);
      unawaited(finish());
      return;
    }
    state = state.copyWith(remainingSeconds: seconds);
  }
}
