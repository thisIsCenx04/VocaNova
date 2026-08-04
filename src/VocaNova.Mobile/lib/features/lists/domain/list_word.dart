class ListWord {
  const ListWord({
    required this.wordId,
    required this.word,
    required this.correctCount,
    required this.wrongCount,
    required this.addedAt,
    this.primaryMeaning,
    this.note,
  });

  final int wordId;
  final String word;
  final String? primaryMeaning;
  final int correctCount;
  final int wrongCount;
  final String? note;
  final DateTime addedAt;

  factory ListWord.fromJson(Map<String, dynamic> json) => ListWord(
    wordId: json['word_id'] as int,
    word: json['word'] as String,
    primaryMeaning: json['primary_meaning'] as String?,
    correctCount: json['correct_count'] as int,
    wrongCount: json['wrong_count'] as int,
    note: json['note'] as String?,
    addedAt: DateTime.parse(json['added_at'] as String),
  );

  Map<String, dynamic> toJson() => {
    'word_id': wordId,
    'word': word,
    'primary_meaning': primaryMeaning,
    'correct_count': correctCount,
    'wrong_count': wrongCount,
    'note': note,
    'added_at': addedAt.toIso8601String(),
  };
}

class ListWordsPage {
  const ListWordsPage({
    required this.items,
    required this.page,
    required this.totalPages,
  });

  final List<ListWord> items;
  final int page;
  final int totalPages;
}
