import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/progress/domain/progress_summary.dart';

class ProgressRepository {
  const ProgressRepository({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<ProgressSummary> getSummary() async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.progressSummary,
    );
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw const FormatException('Invalid progress summary response.');
    }
    return ProgressSummary.fromJson(data);
  }
}
