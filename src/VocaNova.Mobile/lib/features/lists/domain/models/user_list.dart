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
}
