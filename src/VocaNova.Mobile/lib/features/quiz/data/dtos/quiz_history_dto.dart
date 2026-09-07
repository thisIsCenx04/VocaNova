import 'package:vocanova_mobile/features/quiz/domain/models/quiz_history.dart';

class QuizHistoryItemDto {
  const QuizHistoryItemDto({
    required this.sessionId,
    required this.answerMethod,
    required this.mode,
    required this.questionType,
    required this.questionCount,
    required this.correctCount,
    required this.wrongCount,
    required this.accuracy,
    required this.score,
    required this.maxStreak,
    required this.status,
    required this.startedAt,
    this.endedAt,
  });

  final int sessionId;
  final String answerMethod;
  final String mode;
  final int questionType;
  final int questionCount;
  final int correctCount;
  final int wrongCount;
  final double accuracy;
  final double score;
  final int maxStreak;
  final String status;
  final DateTime startedAt;
  final DateTime? endedAt;

  factory QuizHistoryItemDto.fromJson(Map<String, dynamic> json) =>
      QuizHistoryItemDto(
        sessionId: json['session_id'] as int,
        answerMethod: json['answer_method'] as String,
        mode: json['mode'] as String,
        questionType: json['question_type'] as int,
        questionCount: json['question_count'] as int,
        correctCount: json['correct_count'] as int,
        wrongCount: json['wrong_count'] as int,
        accuracy: (json['accuracy'] as num).toDouble(),
        score: (json['score'] as num).toDouble(),
        maxStreak: json['max_streak'] as int,
        status: json['status'] as String,
        startedAt: DateTime.parse(json['started_at'] as String).toLocal(),
        endedAt: json['ended_at'] == null
            ? null
            : DateTime.parse(json['ended_at'] as String).toLocal(),
      );

  QuizHistoryItem toDomain() => QuizHistoryItem(
    sessionId: sessionId,
    answerMethod: answerMethod,
    mode: mode,
    questionType: questionType,
    questionCount: questionCount,
    correctCount: correctCount,
    wrongCount: wrongCount,
    accuracy: accuracy,
    score: score,
    maxStreak: maxStreak,
    status: status,
    startedAt: startedAt,
    endedAt: endedAt,
  );
}
