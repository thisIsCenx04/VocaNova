import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_result.dart';

part 'quiz_result_notifier.g.dart';

@riverpod
Future<QuizResult> quizResult(Ref ref, int sessionId) =>
    ref.read(quizRepositoryProvider).getResult(sessionId);
