import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/app/router/app_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/core/connectivity/cache_warming_service.dart';
import 'package:vocanova_mobile/core/network/app_exception.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/secure_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/auth/data/services/auth_api_service.dart';
import 'package:vocanova_mobile/features/auth/data/dtos/user_profile_dto.dart';
import 'package:vocanova_mobile/features/auth/data/services/google_auth_service.dart';
import 'package:vocanova_mobile/features/auth/domain/models/auth_state.dart';
import 'package:vocanova_mobile/features/auth/domain/models/auth_tokens.dart';
import 'package:vocanova_mobile/features/auth/domain/models/user_profile.dart';
import 'package:vocanova_mobile/features/lists/data/services/lists_api_service.dart';
import 'package:vocanova_mobile/features/progress/data/services/progress_api_service.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

part 'auth_notifier.g.dart';

@Riverpod(keepAlive: true)
AuthApiService authRepository(Ref ref) {
  return AuthApiService(dio: DioClient.instance.dio);
}

final authApiServiceProvider = authRepositoryProvider;

@Riverpod(keepAlive: true)
GoogleAuthService googleAuthService(Ref ref) => GoogleAuthService();

@Riverpod(keepAlive: true)
LocalStorage localStorage(Ref ref) => LocalStorage.instance;

@Riverpod(keepAlive: true)
SecureStorage secureStorage(Ref ref) => SecureStorage.instance;

@Riverpod(keepAlive: true)
AppRouter appRouter(Ref ref) => AppRouter.instance;

@Riverpod(keepAlive: true)
CacheWarmingService cacheWarmingService(Ref ref) => CacheWarmingService(
  listsApiService: ListsApiService(dio: DioClient.instance.dio),
  progressApiService: ProgressApiService(dio: DioClient.instance.dio),
  storage: ref.read(localStorageProvider),
);

@Riverpod(keepAlive: true)
class AuthNotifier extends _$AuthNotifier {
  static const profileCacheTtl = Duration(days: 1);

  @override
  AuthState build() => const AuthState();

  Future<bool> register({
    required String phone,
    required String password,
    required String displayName,
    required String otpCode,
    DateTime? dateOfBirth,
    int? regionId,
    int? occupationId,
    int? educationLevelId,
  }) async {
    return _authenticate(
      () => ref
          .read(authApiServiceProvider)
          .register(
            phone: phone,
            password: password,
            displayName: displayName,
            otpCode: otpCode,
            dateOfBirth: dateOfBirth,
            regionId: regionId,
            occupationId: occupationId,
            educationLevelId: educationLevelId,
          ),
    );
  }

  Future<void> login(String phone, String password) async {
    await _authenticate(
      () => ref
          .read(authApiServiceProvider)
          .login(phone: phone, password: password),
    );
  }

  Future<void> googleLogin(String idToken) async {
    await _authenticate(
      () => ref.read(authApiServiceProvider).googleLogin(idToken),
    );
  }

  Future<void> signInWithGoogle() async {
    state = state.copyWith(status: AuthStatus.loading, clearError: true);
    try {
      final idToken = await ref.read(googleAuthServiceProvider).getIdToken();
      await _authenticate(
        () => ref.read(authApiServiceProvider).googleLogin(idToken),
      );
    } catch (error) {
      state = AuthState(
        status: AuthStatus.error,
        errorMessage: _errorMessage(error),
      );
    }
  }

  Future<void> loadCurrentUser() async {
    final secureStorage = ref.read(secureStorageProvider);
    final accessToken = await secureStorage.getAccessToken();
    if (accessToken == null || accessToken.isEmpty) {
      state = const AuthState(status: AuthStatus.unauthenticated);
      return;
    }

    state = state.copyWith(status: AuthStatus.loading, clearError: true);
    try {
      final user = await ref.read(authApiServiceProvider).getCurrentUser();
      await _cacheUser(user);
      state = AuthState(status: AuthStatus.authenticated, user: user);
    } catch (error) {
      final cachedUser = await _readCachedUser();
      if (cachedUser != null) {
        state = AuthState(status: AuthStatus.authenticated, user: cachedUser);
        return;
      }
      state = AuthState(
        status: AuthStatus.error,
        errorMessage: _errorMessage(error),
      );
    }
  }

  Future<bool> updateLearningProfile(LearningProfile profile) async {
    state = state.copyWith(status: AuthStatus.loading, clearError: true);
    try {
      final user = await ref
          .read(authApiServiceProvider)
          .updateLearningProfile(profile);
      await _cacheUser(user);
      state = AuthState(status: AuthStatus.authenticated, user: user);
      return true;
    } catch (error) {
      state = AuthState(
        status: AuthStatus.error,
        user: state.user,
        errorMessage: _errorMessage(error),
      );
      return false;
    }
  }

  Future<bool> updateProfile({
    required String displayName,
    String? avatarUrl,
  }) async {
    state = state.copyWith(status: AuthStatus.loading, clearError: true);
    try {
      final user = await ref
          .read(authApiServiceProvider)
          .updateProfile(displayName: displayName, avatarUrl: avatarUrl);
      await _cacheUser(user);
      state = AuthState(status: AuthStatus.authenticated, user: user);
      return true;
    } catch (error) {
      state = AuthState(
        status: AuthStatus.error,
        user: state.user,
        errorMessage: _errorMessage(error),
      );
      return false;
    }
  }

  Future<bool> uploadAvatar({
    required Uint8List bytes,
    required String fileName,
  }) async {
    state = state.copyWith(status: AuthStatus.loading, clearError: true);
    try {
      final user = await ref
          .read(authApiServiceProvider)
          .uploadAvatar(bytes: bytes, fileName: fileName);
      await _cacheUser(user);
      state = AuthState(status: AuthStatus.authenticated, user: user);
      return true;
    } catch (error) {
      state = AuthState(
        status: AuthStatus.error,
        user: state.user,
        errorMessage: _errorMessage(error),
      );
      return false;
    }
  }

  Future<bool> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    state = state.copyWith(status: AuthStatus.loading, clearError: true);
    try {
      final changed = await ref
          .read(authApiServiceProvider)
          .changePassword(
            currentPassword: currentPassword,
            newPassword: newPassword,
          );
      state = AuthState(status: AuthStatus.authenticated, user: state.user);
      return changed;
    } catch (error) {
      state = AuthState(
        status: AuthStatus.error,
        user: state.user,
        errorMessage: _errorMessage(error),
      );
      return false;
    }
  }

  Future<void> logout() async {
    final secureStorage = ref.read(secureStorageProvider);
    final refreshToken = await secureStorage.getRefreshToken();
    try {
      if (refreshToken != null && refreshToken.isNotEmpty) {
        await ref.read(authApiServiceProvider).logout(refreshToken);
      }
    } catch (_) {
      // Local logout must succeed even when the API is unavailable.
    } finally {
      try {
        await ref.read(googleAuthServiceProvider).signOut();
      } catch (_) {
        // Backend and local logout must still succeed if Google is unavailable.
      }
      await secureStorage.clearTokens();
      await ref.read(localStorageProvider).clearAll();
      state = const AuthState(status: AuthStatus.unauthenticated);
      ref.read(appRouterProvider).router.go(AppRoutes.login);
    }
  }

  Future<bool> deleteAccount() async {
    state = state.copyWith(status: AuthStatus.loading, clearError: true);
    try {
      await ref.read(authApiServiceProvider).deleteAccount();
      await ref.read(secureStorageProvider).clearTokens();
      await ref.read(localStorageProvider).clearAll();
      state = const AuthState(status: AuthStatus.unauthenticated);
      ref.read(appRouterProvider).router.go(AppRoutes.login);
      return true;
    } catch (error) {
      state = AuthState(
        status: AuthStatus.error,
        user: state.user,
        errorMessage: _errorMessage(error),
      );
      return false;
    }
  }

  Future<bool> _authenticate(Future<AuthTokens> Function() request) async {
    state = state.copyWith(status: AuthStatus.loading, clearError: true);
    try {
      final tokens = await request();
      await ref
          .read(secureStorageProvider)
          .saveTokens(
            accessToken: tokens.accessToken,
            refreshToken: tokens.refreshToken,
          );
      await loadCurrentUser();
      if (state.status == AuthStatus.authenticated) {
        await ref.read(cacheWarmingServiceProvider).warm();
      }
      return state.status == AuthStatus.authenticated;
    } catch (error) {
      state = AuthState(
        status: AuthStatus.error,
        errorMessage: _errorMessage(error),
      );
      return false;
    }
  }

  Future<void> _cacheUser(UserProfile user) {
    return ref
        .read(localStorageProvider)
        .setWithTtl(
          StorageKeys.userProfileJson,
          jsonEncode(UserProfileDto.fromDomain(user).toJson()),
        );
  }

  Future<UserProfile?> _readCachedUser() async {
    final cached = await ref
        .read(localStorageProvider)
        .getWithTtl<String>(StorageKeys.userProfileJson, ttl: profileCacheTtl);
    if (cached == null) {
      return null;
    }

    try {
      return UserProfileDto.fromJson(
        jsonDecode(cached) as Map<String, dynamic>,
      ).toDomain();
    } on FormatException {
      await ref.read(localStorageProvider).remove(StorageKeys.userProfileJson);
      return null;
    }
  }

  String _errorMessage(Object error) {
    if (error is AppException) {
      return error.message;
    }
    if (error is DioException && error.error is AppException) {
      return (error.error! as AppException).message;
    }
    if (error is FormatException) {
      return error.message;
    }
    final l10n = lookupAppLocalizations(
      AppSettingsNotifier.instance.state.locale,
    );
    return l10n.authGenericError;
  }
}
