import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/features/home/data/services/home_topics_api_service.dart';
import 'package:vocanova_mobile/features/home/domain/models/personal_topic_recommendation.dart';

final homeTopicsApiServiceProvider = Provider<HomeTopicsApiService>(
  (ref) => HomeTopicsApiService(dio: DioClient.instance.dio),
);

/// Loads the public topic catalogue for the Home "Topics for you" section.
/// Kept separate from the search flow so the Home screen can show/refresh it
/// independently, with its own loading/error states.
final homeTopicsProvider =
    FutureProvider.autoDispose<List<PersonalTopicRecommendation>>((ref) async {
      return ref.read(homeTopicsApiServiceProvider).recommendedPersonalTopics();
    });
