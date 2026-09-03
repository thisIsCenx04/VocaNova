import 'package:vocanova_mobile/features/quiz/domain/models/quiz_result.dart';

class QuizResultDto {
  const QuizResultDto({
    required this.sessionId,
    required this.status,
    required this.correctCount,
    required this.wrongCount,
    required this.questionCount,
    required this.answeredCount,
    required this.accuracy,
    required this.maxStreak,
    required this.score,
    required this.answers,
    this.durationSec,
  });

  final int sessionId;
  final String status;
  final int correctCount;
  final int wrongCount;
  final int questionCount;
  final int answeredCount;
  final double accuracy;
  final int? durationSec;
  final int maxStreak;
  final double score;
  final List<QuizResultAnswerDto> answers;

  factory QuizResultDto.fromJson(Map<String, dynamic> json) => QuizResultDto(
    sessionId: json['session_id'] as int,
    status: json['status'] as String,
    correctCount: json['correct_count'] as int,
    wrongCount: json['wrong_count'] as int,
    questionCount: json['question_count'] as int,
    answeredCount: json['answered_count'] as int,
    accuracy: (json['accuracy'] as num).toDouble(),
    durationSec: json['duration_sec'] as int?,
    maxStreak: json['max_streak'] as int,
    score: (json['score'] as num).toDouble(),
    answers: (json['answers'] as List<dynamic>)
        .whereType<Map<String, dynamic>>()
        .map(QuizResultAnswerDto.fromJson)
        .toList(growable: false),
  );

  QuizResult toDomain() => QuizResult(
    sessionId: sessionId,
    status: status,
    correctCount: correctCount,
    wrongCount: wrongCount,
    questionCount: questionCount,
    answeredCount: answeredCount,
    accuracy: accuracy,
    durationSec: durationSec,
    maxStreak: maxStreak,
    score: score,
    answers: answers.map((dto) => dto.toDomain()).toList(growable: false),
  );
}

class QuizResultAnswerDto {
  const QuizResultAnswerDto({
    required this.wordId,
    required this.questionNumber,
    required this.displayContent,
    required this.expectedAnswer,
    required this.isCorrect,
    this.userAnswer,
  });

  final int wordId;
  final int questionNumber;
  final String displayContent;
  final String expectedAnswer;
  final String? userAnswer;
  final bool? isCorrect;

  factory QuizResultAnswerDto.fromJson(Map<String, dynamic> json) =>
      QuizResultAnswerDto(
        wordId: json['word_id'] as int,
        questionNumber: json['question_number'] as int,
        displayContent: json['display_content'] as String,
        expectedAnswer: json['expected_answer'] as String,
        userAnswer: json['user_answer'] as String?,
        isCorrect: json['is_correct'] as bool?,
      );

  QuizResultAnswer toDomain() => QuizResultAnswer(
    wordId: wordId,
    questionNumber: questionNumber,
    displayContent: displayContent,
    expectedAnswer: expectedAnswer,
    userAnswer: userAnswer,
    isCorrect: isCorrect,
  );
}

class WrongWordDto {
  const WrongWordDto({
    required this.wordId,
    required this.word,
    required this.testCount,
    required this.correctCount,
    required this.wrongCount,
    required this.masteryLevel,
    this.primaryMeaning,
  });

  final int wordId;
  final String word;
  final String? primaryMeaning;
  final int testCount;
  final int correctCount;
  final int wrongCount;
  final int masteryLevel;

  factory WrongWordDto.fromJson(Map<String, dynamic> json) => WrongWordDto(
    wordId: json['word_id'] as int,
    word: json['word'] as String,
    primaryMeaning: json['primary_meaning'] as String?,
    testCount: json['test_count'] as int,
    correctCount: json['correct_count'] as int,
    wrongCount: json['wrong_count'] as int,
    masteryLevel: json['mastery_level'] as int,
  );

  WrongWord toDomain() => WrongWord(
    wordId: wordId,
    word: word,
    primaryMeaning: primaryMeaning,
    testCount: testCount,
    correctCount: correctCount,
    wrongCount: wrongCount,
    masteryLevel: masteryLevel,
  );
}
