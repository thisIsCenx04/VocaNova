import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';

class WordSearchRepository {
  const WordSearchRepository({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<List<WordSummary>> search({
    required String query,
    String? cefr,
    int? topicId,
  }) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.words,
      queryParameters: {
        'q': query,
        'page': 1,
        'limit': 50,
        'cefr': ?cefr,
        'topicId': ?topicId,
      },
    );
    return _dataList(
      response,
    ).map(WordSummary.fromJson).toList(growable: false);
  }

  Future<List<TopicSummary>> getTopics() async {
    final response = await _dio.get<Map<String, dynamic>>(ApiEndpoints.topics);
    return _dataList(
      response,
    ).map(TopicSummary.fromJson).toList(growable: false);
  }

  Future<List<PersonalTopicSummary>> getPersonalTopics({int? wordId}) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.personalTopics,
      queryParameters: {'wordId': ?wordId},
    );
    return _dataList(
      response,
    ).map(PersonalTopicSummary.fromJson).toList(growable: false);
  }

  Future<List<WordSummary>> getTopicWords(int topicId) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.topicWords(topicId),
      queryParameters: const {'page': 1, 'limit': 100},
    );
    return _dataList(response)
        .map(WordSummary.fromJson)
        .map((word) => word.withTopic(topicId))
        .toList(growable: false);
  }

  Future<List<WordSummary>> getPersonalTopicWords(int topicId) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.personalTopicWords(topicId),
      queryParameters: const {'page': 1, 'limit': 100},
    );
    return _dataList(response)
        .map(WordSummary.fromJson)
        .map((word) => word.withTopic(topicId))
        .toList(growable: false);
  }

  Future<void> removePersonalTopicWord({
    required int topicId,
    required int wordId,
  }) async {
    await _dio.delete<Map<String, dynamic>>(
      ApiEndpoints.personalTopicWord(topicId, wordId),
    );
  }

  List<Map<String, dynamic>> _dataList(
    Response<Map<String, dynamic>> response,
  ) {
    final payload = response.data?['data'];
    final data = switch (payload) {
      List<dynamic> value => value,
      Map<String, dynamic> value => value['items'] ?? value['Items'],
      _ => null,
    };
    if (data is! List<dynamic>) {
      throw const FormatException('API response data is invalid.');
    }
    return data.whereType<Map<String, dynamic>>().toList(growable: false);
  }
}
