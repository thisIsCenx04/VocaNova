import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/lists/domain/list_word.dart';
import 'package:vocanova_mobile/features/lists/domain/user_list.dart';

class ListsRepository {
  const ListsRepository({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<List<UserList>> getLists() async {
    final response = await _dio.get<Map<String, dynamic>>(ApiEndpoints.lists);
    final data = response.data?['data'];
    if (data is! List) throw const FormatException('Invalid lists response.');
    return data
        .whereType<Map<String, dynamic>>()
        .map(UserList.fromJson)
        .toList(growable: false);
  }

  Future<UserList> create(String name) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.lists,
      data: {'list_name': name},
    );
    return UserList.fromJson(_dataMap(response));
  }

  Future<UserList> rename({required int listId, required String name}) async {
    final response = await _dio.put<Map<String, dynamic>>(
      ApiEndpoints.list(listId),
      data: {'list_name': name},
    );
    return UserList.fromJson(_dataMap(response));
  }

  Future<void> delete(int listId) async {
    await _dio.delete<Map<String, dynamic>>(ApiEndpoints.list(listId));
  }

  Future<ListWordsPage> getWords({
    required int listId,
    required int page,
  }) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.listWords(listId),
      queryParameters: {'page': page, 'limit': 20},
    );
    final data = _dataMap(response);
    final items = data['items'];
    if (items is! List) throw const FormatException('Invalid words response.');
    return ListWordsPage(
      items: items
          .whereType<Map<String, dynamic>>()
          .map(ListWord.fromJson)
          .toList(growable: false),
      page: data['page'] as int,
      totalPages: data['totalPages'] as int,
    );
  }

  Future<ListWord> addWord({required int listId, required int wordId}) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.listWords(listId),
      data: {'word_id': wordId, 'add_method': 'search', 'note': null},
    );
    return ListWord.fromJson(_dataMap(response));
  }

  Future<List<ListWord>> addRandomWords({
    required int listId,
    required int count,
    required String method,
    int? topicId,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.listRandomWords(listId),
      data: {'topic_id': topicId, 'count': count, 'method': method},
    );
    final words = _dataMap(response)['words'];
    if (words is! List) throw const FormatException('Invalid random response.');
    return words
        .whereType<Map<String, dynamic>>()
        .map(ListWord.fromJson)
        .toList(growable: false);
  }

  Future<void> removeWord({required int listId, required int wordId}) async {
    await _dio.delete<Map<String, dynamic>>(
      ApiEndpoints.listWord(listId, wordId),
    );
  }

  Map<String, dynamic> _dataMap(Response<Map<String, dynamic>> response) {
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw const FormatException('Invalid list response.');
    }
    return data;
  }
}
