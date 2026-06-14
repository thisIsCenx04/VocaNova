import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/auth/domain/user_profile.dart';

class AuthRepository {
  const AuthRepository({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<AuthTokens> register({
    required String phone,
    required String password,
    required String displayName,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.register,
      data: {'phone': phone, 'password': password, 'display_name': displayName},
    );
    return AuthTokens.fromJson(_responseData(response));
  }

  Future<AuthTokens> login({
    required String phone,
    required String password,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.login,
      data: {'phone': phone, 'password': password},
    );
    return AuthTokens.fromJson(_responseData(response));
  }

  Future<AuthTokens> googleLogin(String idToken) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.googleLogin,
      data: {'id_token': idToken},
    );
    return AuthTokens.fromJson(_responseData(response));
  }

  Future<int> sendOtp({
    required String phone,
    String purpose = 'verify',
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.sendOtp,
      data: {'phone': phone, 'purpose': purpose},
    );
    return _responseData(response)['expires_in'] as int;
  }

  Future<bool> verifyOtp({
    required String phone,
    required String otpCode,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.verifyOtp,
      data: {'phone': phone, 'otp_code': otpCode},
    );
    return _responseData(response)['verified'] as bool;
  }

  Future<int> forgotPassword(String phone) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.forgotPassword,
      data: {'phone': phone},
    );
    return _responseData(response)['expires_in'] as int;
  }

  Future<bool> resetPassword({
    required String phone,
    required String otpCode,
    required String newPassword,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.resetPassword,
      data: {'phone': phone, 'otp_code': otpCode, 'new_password': newPassword},
    );
    final data = response.data?['data'];
    if (data is! bool) {
      throw const FormatException('API response data is invalid.');
    }
    return data;
  }

  Future<UserProfile> getCurrentUser() async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.currentUser,
    );
    return UserProfile.fromJson(_responseData(response));
  }

  Future<UserProfile> updateLearningProfile(LearningProfile profile) async {
    final response = await _dio.put<Map<String, dynamic>>(
      ApiEndpoints.updateLearningProfile,
      data: profile.toJson(),
    );
    return UserProfile.fromJson(_responseData(response));
  }

  Future<void> logout(String refreshToken) async {
    await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.logout,
      data: {'refresh_token': refreshToken},
    );
  }

  Map<String, dynamic> _responseData(Response<Map<String, dynamic>> response) {
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw const FormatException('API response data is invalid.');
    }
    return data;
  }
}

class AuthTokens {
  const AuthTokens({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresIn,
    required this.tokenType,
  });

  final String accessToken;
  final String refreshToken;
  final int expiresIn;
  final String tokenType;

  factory AuthTokens.fromJson(Map<String, dynamic> json) {
    return AuthTokens(
      accessToken: json['access_token'] as String,
      refreshToken: json['refresh_token'] as String,
      expiresIn: json['expires_in'] as int,
      tokenType: json['token_type'] as String? ?? 'Bearer',
    );
  }
}
