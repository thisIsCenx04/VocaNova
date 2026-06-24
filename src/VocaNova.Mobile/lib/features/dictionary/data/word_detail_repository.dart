import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_detail.dart';

class WordDetailRepository {
  const WordDetailRepository({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<WordDetail> getWord(int wordId) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.word(wordId),
    );
    return WordDetail.fromJson(_dataMap(response));
  }

  Future<List<UserListSummary>> getLists() async {
    final response = await _dio.get<Map<String, dynamic>>(ApiEndpoints.lists);
    final data = response.data?['data'];
    if (data is! List) throw const FormatException('Invalid lists response.');
    return data
        .whereType<Map<String, dynamic>>()
        .map(UserListSummary.fromJson)
        .toList(growable: false);
  }

  Future<UserListSummary> createList(String name) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.lists,
      data: {'list_name': name},
    );
    return UserListSummary.fromJson(_dataMap(response));
  }

  Future<void> addWordToList({
    required int listId,
    required int wordId,
    String? note,
  }) async {
    await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.listWords(listId),
      data: {
        'word_id': wordId,
        'add_method': 'manual',
        'note': note?.trim().isEmpty == true ? null : note,
      },
    );
  }

  Map<String, dynamic> _dataMap(Response<Map<String, dynamic>> response) {
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw const FormatException('Invalid word detail response.');
    }
    return data;
  }
}
