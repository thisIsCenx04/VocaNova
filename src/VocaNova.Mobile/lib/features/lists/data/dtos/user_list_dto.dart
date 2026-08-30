import 'package:vocanova_mobile/features/lists/domain/models/user_list.dart';

class UserListDto {
  const UserListDto({
    required this.listId,
    required this.listName,
    required this.wordCount,
    required this.createdAt,
  });

  final int listId;
  final String listName;
  final int wordCount;
  final DateTime createdAt;

  factory UserListDto.fromJson(Map<String, dynamic> json) => UserListDto(
    listId: json['list_id'] as int,
    listName: json['list_name'] as String,
    wordCount: json['word_count'] as int,
    createdAt: DateTime.parse(json['created_at'] as String),
  );

  factory UserListDto.fromDomain(UserList list) => UserListDto(
    listId: list.listId,
    listName: list.listName,
    wordCount: list.wordCount,
    createdAt: list.createdAt,
  );

  UserList toDomain() => UserList(
    listId: listId,
    listName: listName,
    wordCount: wordCount,
    createdAt: createdAt,
  );

  Map<String, dynamic> toJson() => {
    'list_id': listId,
    'list_name': listName,
    'word_count': wordCount,
    'created_at': createdAt.toIso8601String(),
  };
}
