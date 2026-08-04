import 'package:vocanova_mobile/features/quiz/domain/quiz_config.dart';

class QuizSessionState {
  const QuizSessionState({
    required this.question,
    required this.questionNumber,
    required this.questionCount,
    required this.remainingLives,
    required this.remainingSeconds,
    this.selectedAnswer,
    this.answerResult,
    this.isSubmitting = false,
    this.isFinishing = false,
    this.isFinished = false,
    this.errorMessage,
  });

  final QuizQuestion question;
  final int questionNumber;
  final int questionCount;
  final int? remainingLives;
  final int? remainingSeconds;
  final String? selectedAnswer;
  final QuizAnswerResult? answerResult;
  final bool isSubmitting;
  final bool isFinishing;
  final bool isFinished;
  final String? errorMessage;

  bool get hasAnswered => answerResult != null;

  QuizSessionState copyWith({
    QuizQuestion? question,
    int? questionNumber,
    int? questionCount,
    int? remainingLives,
    int? remainingSeconds,
    String? selectedAnswer,
    QuizAnswerResult? answerResult,
    bool? isSubmitting,
    bool? isFinishing,
    bool? isFinished,
    String? errorMessage,
    bool clearAnswer = false,
    bool clearError = false,
  }) => QuizSessionState(
    question: question ?? this.question,
    questionNumber: questionNumber ?? this.questionNumber,
    questionCount: questionCount ?? this.questionCount,
    remainingLives: remainingLives ?? this.remainingLives,
    remainingSeconds: remainingSeconds ?? this.remainingSeconds,
    selectedAnswer: clearAnswer ? null : selectedAnswer ?? this.selectedAnswer,
    answerResult: clearAnswer ? null : answerResult ?? this.answerResult,
    isSubmitting: isSubmitting ?? this.isSubmitting,
    isFinishing: isFinishing ?? this.isFinishing,
    isFinished: isFinished ?? this.isFinished,
    errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
  );
}
