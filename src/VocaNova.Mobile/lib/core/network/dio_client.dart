import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/interceptors/auth_interceptor.dart';
import 'package:vocanova_mobile/core/network/interceptors/error_interceptor.dart';
import 'package:vocanova_mobile/core/storage/secure_storage.dart';
import 'package:vocanova_mobile/core/storage/token_storage.dart';

class DioClient {
  DioClient._({
    required String baseUrl,
    required TokenStorage tokenStorage,
    LogoutCallback? onLogout,
    HttpClientAdapter? adapter,
    HttpClientAdapter? refreshAdapter,
  }) {
    final options = BaseOptions(
      baseUrl: baseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 30),
      headers: const {'Accept': 'application/json'},
    );

    _dio = Dio(options);
    _refreshDio = Dio(options);
    if (adapter != null) {
      _dio.httpClientAdapter = adapter;
    }
    if (refreshAdapter != null) {
      _refreshDio.httpClientAdapter = refreshAdapter;
    }

    _dio.interceptors.addAll([
      AuthInterceptor(
        dio: _dio,
        refreshDio: _refreshDio,
        tokenStorage: tokenStorage,
        onLogout: onLogout,
      ),
      ErrorInterceptor(),
    ]);
  }

  static const defaultBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5013',
  );

  static final DioClient instance = DioClient._(
    baseUrl: defaultBaseUrl,
    tokenStorage: SecureStorage.instance,
  );

  late final Dio _dio;
  late final Dio _refreshDio;

  Dio get dio => _dio;

  static DioClient create({
    required String baseUrl,
    required TokenStorage tokenStorage,
    LogoutCallback? onLogout,
    HttpClientAdapter? adapter,
    HttpClientAdapter? refreshAdapter,
  }) {
    return DioClient._(
      baseUrl: baseUrl,
      tokenStorage: tokenStorage,
      onLogout: onLogout,
      adapter: adapter,
      refreshAdapter: refreshAdapter,
    );
  }
}
