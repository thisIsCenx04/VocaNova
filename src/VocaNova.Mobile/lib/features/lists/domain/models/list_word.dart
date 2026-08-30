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
