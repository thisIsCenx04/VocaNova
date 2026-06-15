class QuizSessionStart {
  const QuizSessionStart({
    required this.sessionId,
    required this.firstQuestionWordId,
  });

  final int sessionId;
  final int firstQuestionWordId;

  factory QuizSessionStart.fromJson(Map<String, dynamic> json) {
    final session = json['session'] as Map<String, dynamic>;
    final question = json['first_question'] as Map<String, dynamic>;
    return QuizSessionStart(
      sessionId: session['session_id'] as int,
      firstQuestionWordId: question['word_id'] as int,
    );
  }
}

class QuizConfigRequest {
  const QuizConfigRequest({
    required this.mode,
    required this.questionType,
    required this.scopeType,
    required this.topicIds,
    required this.answerMethod,
    this.scopeDateFrom,
    this.scopeDateTo,
    this.timeLimitSec,
    this.lives,
  });

  final String mode;
  final int questionType;
  final String scopeType;
  final DateTime? scopeDateFrom;
  final DateTime? scopeDateTo;
  final List<int> topicIds;
  final int? timeLimitSec;
  final int? lives;
  final String answerMethod;

  Map<String, dynamic> toJson() => {
    'mode': mode,
    'question_type': questionType,
    'scope_type': scopeType,
    'scope_date_from': _date(scopeDateFrom),
    'scope_date_to': _date(scopeDateTo),
    'topic_ids': topicIds.isEmpty ? null : topicIds,
    'word_order': 'random',
    'word_limit': 20,
    'time_limit_sec': timeLimitSec,
    'lives': lives,
    'answer_method': answerMethod,
  };

  static String? _date(DateTime? value) {
    if (value == null) return null;
    return '${value.year.toString().padLeft(4, '0')}-'
        '${value.month.toString().padLeft(2, '0')}-'
        '${value.day.toString().padLeft(2, '0')}';
  }
}
