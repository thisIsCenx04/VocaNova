import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/dictionary/data/dtos/word_detail_dto.dart';
import 'package:vocanova_mobile/features/dictionary/data/dtos/word_summary_dto.dart';
import 'package:vocanova_mobile/features/dictionary/domain/models/word_detail.dart';
import 'package:vocanova_mobile/features/dictionary/domain/models/word_summary.dart';

class WordDetailApiService {
  const WordDetailApiService({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<WordDetail> getWord(int wordId) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.word(wordId),
    );
    return WordDetailDto.fromJson(_dataMap(response)).toDomain();
  }

  Future<WordDetail> getDailyWord() async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.dailyWord,
    );
    return WordDetailDto.fromJson(_dataMap(response)).toDomain();
  }

  Future<List<UserListSummary>> getLists() async {
    final response = await _dio.get<Map<String, dynamic>>(ApiEndpoints.lists);
    final data = response.data?['data'];
    if (data is! List) throw const FormatException('Invalid lists response.');
    return data
        .whereType<Map<String, dynamic>>()
        .map(UserListSummaryDto.fromJson)
        .map((dto) => dto.toDomain())
        .toList(growable: false);
  }

  Future<UserListSummary> createList(String name) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.lists,
      data: {'list_name': name},
    );
    return UserListSummaryDto.fromJson(_dataMap(response)).toDomain();
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

  Future<List<PersonalTopicSummary>> getPersonalTopics({int? wordId}) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.personalTopics,
      queryParameters: {'wordId': ?wordId},
    );
    final data = response.data?['data'];
    if (data is! List) {
      throw const FormatException('Invalid personal topics response.');
    }
    return data
        .whereType<Map<String, dynamic>>()
        .map(PersonalTopicSummaryDto.fromJson)
        .map((dto) => dto.toDomain())
        .toList(growable: false);
  }

  Future<PersonalTopicSummary> addWordToPersonalTopic({
    required int topicId,
    required int wordId,
    String? note,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.personalTopicWords(topicId),
      data: {
        'word_id': wordId,
        'note': note?.trim().isEmpty == true ? null : note,
      },
    );
    return PersonalTopicSummaryDto.fromJson(_dataMap(response)).toDomain();
  }

  Map<String, dynamic> _dataMap(Response<Map<String, dynamic>> response) {
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw const FormatException('Invalid word detail response.');
    }
    return data;
  }
}

typedef WordDetailRepository = WordDetailApiService;
