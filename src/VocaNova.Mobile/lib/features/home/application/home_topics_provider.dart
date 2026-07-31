import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/features/home/domain/personal_topic_recommendation.dart';

/// Loads the public topic catalogue for the Home "Topics for you" section.
/// Kept separate from the search flow so the Home screen can show/refresh it
/// independently, with its own loading/error states.
final homeTopicsProvider =
    FutureProvider.autoDispose<List<PersonalTopicRecommendation>>((ref) async {
      final response = await DioClient.instance.dio.get<Map<String, dynamic>>(
        ApiEndpoints.recommendedPersonalTopics,
        queryParameters: const {'limit': 6},
      );
      final data = response.data?['data'];
      if (data is! List<dynamic>) {
        throw const FormatException('Invalid personal topic recommendations.');
      }
      return data
          .whereType<Map<String, dynamic>>()
          .map(PersonalTopicRecommendation.fromJson)
          .toList(growable: false);
    });
