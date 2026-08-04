import 'dart:async';

import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/core/storage/token_storage.dart';

typedef LogoutCallback = FutureOr<void> Function();

class AuthInterceptor extends QueuedInterceptor {
  AuthInterceptor({
    required Dio dio,
    required Dio refreshDio,
    required TokenStorage tokenStorage,
    LogoutCallback? onLogout,
  }) : _dio = dio,
       _refreshDio = refreshDio,
       _tokenStorage = tokenStorage,
       _onLogout = onLogout;

  static const skipAuthKey = 'skipAuth';
  static const retriedKey = 'authRetried';

  final Dio _dio;
  final Dio _refreshDio;
  final TokenStorage _tokenStorage;
  final LogoutCallback? _onLogout;

  Future<bool>? _refreshing;

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    if (options.extra[skipAuthKey] == true) {
      handler.next(options);
      return;
    }

    final accessToken = await _tokenStorage.getAccessToken();
    if (accessToken != null && accessToken.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $accessToken';
    }

    handler.next(options);
  }

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    final request = err.requestOptions;
    final shouldRefresh =
        err.response?.statusCode == 401 &&
        request.extra[skipAuthKey] != true &&
        request.extra[retriedKey] != true &&
        request.path != ApiEndpoints.refreshToken;

    if (!shouldRefresh) {
      handler.next(err);
      return;
    }

    final refreshed = await _refreshAccessToken();
    if (!refreshed) {
      await _logout();
      handler.next(err);
      return;
    }

    final accessToken = await _tokenStorage.getAccessToken();
    final retryOptions = request.copyWith(
      headers: {
        ...request.headers,
        if (accessToken != null) 'Authorization': 'Bearer $accessToken',
      },
      extra: {...request.extra, retriedKey: true},
    );

    try {
      handler.resolve(await _dio.fetch<dynamic>(retryOptions));
    } on DioException catch (retryError) {
      handler.next(retryError);
    }
  }

  Future<bool> _refreshAccessToken() {
    final activeRefresh = _refreshing;
    if (activeRefresh != null) {
      return activeRefresh;
    }

    final refresh = _performRefresh();
    _refreshing = refresh;
    return refresh.whenComplete(() => _refreshing = null);
  }

  Future<bool> _performRefresh() async {
    final refreshToken = await _tokenStorage.getRefreshToken();
    if (refreshToken == null || refreshToken.isEmpty) {
      return false;
    }

    try {
      final response = await _refreshDio.post<Map<String, dynamic>>(
        ApiEndpoints.refreshToken,
        data: {'refresh_token': refreshToken},
        options: Options(extra: const {skipAuthKey: true}),
      );
      final responseData = response.data?['data'];
      if (responseData is! Map<String, dynamic>) {
        return false;
      }

      final newAccessToken = responseData['access_token'];
      final newRefreshToken = responseData['refresh_token'];
      if (newAccessToken is! String || newRefreshToken is! String) {
        return false;
      }

      await _tokenStorage.saveTokens(
        accessToken: newAccessToken,
        refreshToken: newRefreshToken,
      );
      return true;
    } on DioException {
      return false;
    }
  }

  Future<void> _logout() async {
    await _tokenStorage.clearTokens();
    await _onLogout?.call();
  }
}
