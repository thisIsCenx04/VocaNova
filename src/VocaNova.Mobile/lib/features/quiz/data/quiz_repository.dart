import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_config.dart';

class QuizRepository {
  const QuizRepository({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<QuizSessionStart> createSession(QuizConfigRequest request) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.quizSessions,
      data: request.toJson(),
    );
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw const FormatException('Invalid quiz session response.');
    }
    return QuizSessionStart.fromJson(data);
  }
}
