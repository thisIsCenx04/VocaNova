import 'package:vocanova_mobile/features/lists/domain/models/list_word.dart';

class ListWordDto {
  const ListWordDto({
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

  factory ListWordDto.fromJson(Map<String, dynamic> json) => ListWordDto(
    wordId: json['word_id'] as int,
    word: json['word'] as String,
    primaryMeaning: json['primary_meaning'] as String?,
    correctCount: json['correct_count'] as int,
    wrongCount: json['wrong_count'] as int,
    note: json['note'] as String?,
    addedAt: DateTime.parse(json['added_at'] as String),
  );

  factory ListWordDto.fromDomain(ListWord word) => ListWordDto(
    wordId: word.wordId,
    word: word.word,
    primaryMeaning: word.primaryMeaning,
    correctCount: word.correctCount,
    wrongCount: word.wrongCount,
    note: word.note,
    addedAt: word.addedAt,
  );

  ListWord toDomain() => ListWord(
    wordId: wordId,
    word: word,
    primaryMeaning: primaryMeaning,
    correctCount: correctCount,
    wrongCount: wrongCount,
    note: note,
    addedAt: addedAt,
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
