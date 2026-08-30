import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/auth/data/dtos/auth_tokens_dto.dart';
import 'package:vocanova_mobile/features/auth/data/dtos/onboarding_catalog_dto.dart';
import 'package:vocanova_mobile/features/auth/data/dtos/user_profile_dto.dart';
import 'package:vocanova_mobile/features/auth/domain/models/auth_tokens.dart';
import 'package:vocanova_mobile/features/auth/domain/models/onboarding_catalog.dart';
import 'package:vocanova_mobile/features/auth/domain/models/user_profile.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

AppLocalizations get _l10n =>
    lookupAppLocalizations(AppSettingsNotifier.instance.state.locale);

class AuthApiService {
  const AuthApiService({required Dio dio}) : _dio = dio;

  final Dio _dio;

  /// The learning-profile fields are optional. When supplied they seed the KNN profile
  /// vector at sign-up, so a brand-new account already has something to match neighbours on.
  Future<AuthTokens> register({
    required String phone,
    required String password,
    required String displayName,
    required String otpCode,
    DateTime? dateOfBirth,
    int? regionId,
    int? occupationId,
    int? educationLevelId,
  }) async {
    final formattedDateOfBirth = dateOfBirth == null
        ? null
        : _formatDate(dateOfBirth);
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.register,
      data: {
        'phone': phone,
        'password': password,
        'display_name': displayName,
        'otp_code': otpCode,
        'date_of_birth': ?formattedDateOfBirth,
        'region_id': ?regionId,
        'occupation_id': ?occupationId,
        'education_level_id': ?educationLevelId,
      },
    );
    return AuthTokensDto.fromJson(_responseData(response)).toDomain();
  }

  Future<LearningProfileOptions> getLearningProfileOptions() async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.learningProfileOptions,
    );
    return LearningProfileOptionsDto.fromJson(
      _responseData(response),
    ).toDomain();
  }

  Future<int> selectOnboardingTopics(List<int> topicIds) async {
    final response = await _dio.put<Map<String, dynamic>>(
      ApiEndpoints.onboardingTopicSelection,
      data: {'topic_ids': topicIds},
    );
    final data = response.data?['data'];
    if (data is! int) {
      throw FormatException(_l10n.authApiResponseInvalid);
    }
    return data;
  }

  static String _formatDate(DateTime date) {
    final month = date.month.toString().padLeft(2, '0');
    final day = date.day.toString().padLeft(2, '0');
    return '${date.year}-$month-$day';
  }

  Future<AuthTokens> login({
    required String phone,
    required String password,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.login,
      data: {'phone': phone, 'password': password},
    );
    return AuthTokensDto.fromJson(_responseData(response)).toDomain();
  }

  Future<AuthTokens> googleLogin(String idToken) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.googleLogin,
      data: {'id_token': idToken},
    );
    return AuthTokensDto.fromJson(_responseData(response)).toDomain();
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

  Future<bool> verifyResetOtp({
    required String phone,
    required String otpCode,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.verifyResetOtp,
      data: {'phone': phone, 'otp_code': otpCode},
    );
    return _responseData(response)['verified'] as bool;
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
      throw FormatException(_l10n.authApiResponseInvalid);
    }
    return data;
  }

  Future<UserProfile> getCurrentUser() async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.currentUser,
    );
    return UserProfileDto.fromJson(_responseData(response)).toDomain();
  }

  Future<UserProfile> updateLearningProfile(LearningProfile profile) async {
    final response = await _dio.put<Map<String, dynamic>>(
      ApiEndpoints.updateLearningProfile,
      data: LearningProfileDto.fromDomain(profile).toJson(),
    );
    return UserProfileDto.fromJson(_responseData(response)).toDomain();
  }

  Future<UserProfile> updateProfile({
    required String displayName,
    String? avatarUrl,
  }) async {
    final response = await _dio.put<Map<String, dynamic>>(
      ApiEndpoints.updateProfile,
      data: {'display_name': displayName, 'avatar_url': avatarUrl},
    );
    return UserProfileDto.fromJson(_responseData(response)).toDomain();
  }

  Future<UserProfile> uploadAvatar({
    required Uint8List bytes,
    required String fileName,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.uploadAvatar,
      data: FormData.fromMap({
        'file': MultipartFile.fromBytes(bytes, filename: fileName),
      }),
    );
    return UserProfileDto.fromJson(_responseData(response)).toDomain();
  }

  Future<bool> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    final response = await _dio.put<Map<String, dynamic>>(
      ApiEndpoints.changePassword,
      data: {'current_password': currentPassword, 'new_password': newPassword},
    );
    final data = response.data?['data'];
    if (data is! bool) {
      throw FormatException(_l10n.authApiResponseInvalid);
    }
    return data;
  }

  Future<void> logout(String refreshToken) async {
    await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.logout,
      data: {'refresh_token': refreshToken},
    );
  }

  Future<void> deleteAccount() async {
    await _dio.delete<Map<String, dynamic>>(ApiEndpoints.deleteAccount);
  }

  Map<String, dynamic> _responseData(Response<Map<String, dynamic>> response) {
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw FormatException(_l10n.authApiResponseInvalid);
    }
    return data;
  }
}

typedef AuthRepository = AuthApiService;
