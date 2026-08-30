import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/home/data/dtos/personal_topic_recommendation_dto.dart';
import 'package:vocanova_mobile/features/home/domain/models/personal_topic_recommendation.dart';

class HomeTopicsApiService {
  const HomeTopicsApiService({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<List<PersonalTopicRecommendation>> recommendedPersonalTopics({
    int limit = 6,
  }) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.recommendedPersonalTopics,
      queryParameters: {'limit': limit},
    );
    final data = response.data?['data'];
    if (data is! List<dynamic>) {
      throw const FormatException('Invalid personal topic recommendations.');
    }
    return data
        .whereType<Map<String, dynamic>>()
        .map(PersonalTopicRecommendationDto.fromJson)
        .map((dto) => dto.toDomain())
        .toList(growable: false);
  }
}
