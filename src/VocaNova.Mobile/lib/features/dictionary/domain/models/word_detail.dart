class WordDetail {
  const WordDetail({
    required this.wordId,
    required this.word,
    required this.isPhrase,
    required this.senses,
    required this.examples,
    required this.relations,
    required this.audio,
    required this.topics,
    this.cefr,
    this.phoneticUk,
    this.phoneticUs,
    this.imageUrl,
  });

  final int wordId;
  final String word;
  final bool isPhrase;
  final String? cefr;
  final String? phoneticUk;
  final String? phoneticUs;
  final String? imageUrl;
  final List<WordSense> senses;
  final List<WordExample> examples;
  final List<WordRelation> relations;
  final List<WordAudio> audio;
  final List<WordTopic> topics;
}

class WordSense {
  const WordSense({
    required this.senseId,
    required this.order,
    required this.wordClass,
    required this.englishDefinition,
    required this.examples,
    required this.relations,
    this.vietnameseMeaning,
  });

  final int senseId;
  final int order;
  final String wordClass;
  final String englishDefinition;
  final String? vietnameseMeaning;
  final List<WordExample> examples;
  final List<WordRelation> relations;
}

class WordExample {
  const WordExample({required this.exampleEn, this.exampleVi});

  final String exampleEn;
  final String? exampleVi;
}

class WordRelation {
  const WordRelation({
    required this.relationType,
    required this.relatedWord,
    this.linkedWordId,
  });

  final String relationType;
  final String relatedWord;
  final int? linkedWordId;
}

class WordAudio {
  const WordAudio({required this.accent, required this.url});

  final String accent;
  final String url;
}

class WordTopic {
  const WordTopic({
    required this.topicId,
    required this.name,
    this.nameVi,
    this.icon,
  });

  final int topicId;
  final String name;
  final String? nameVi;
  final String? icon;
  String get displayName => nameVi?.trim().isNotEmpty == true ? nameVi! : name;
}

class UserListSummary {
  const UserListSummary({
    required this.listId,
    required this.listName,
    required this.wordCount,
  });

  final int listId;
  final String listName;
  final int wordCount;
}
