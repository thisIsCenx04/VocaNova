class UserList {
  const UserList({
    required this.listId,
    required this.listName,
    required this.wordCount,
    required this.createdAt,
  });

  final int listId;
  final String listName;
  final int wordCount;
  final DateTime createdAt;

  factory UserList.fromJson(Map<String, dynamic> json) => UserList(
    listId: json['list_id'] as int,
    listName: json['list_name'] as String,
    wordCount: json['word_count'] as int,
    createdAt: DateTime.parse(json['created_at'] as String),
  );

  Map<String, dynamic> toJson() => {
    'list_id': listId,
    'list_name': listName,
    'word_count': wordCount,
    'created_at': createdAt.toIso8601String(),
  };
}
