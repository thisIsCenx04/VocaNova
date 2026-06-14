import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/app/router/app_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/core/network/app_exception.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/secure_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/auth/data/auth_repository.dart';
import 'package:vocanova_mobile/features/auth/data/google_auth_service.dart';
import 'package:vocanova_mobile/features/auth/domain/auth_state.dart';
import 'package:vocanova_mobile/features/auth/domain/user_profile.dart';

part 'auth_notifier.g.dart';

@Riverpod(keepAlive: true)
AuthRepository authRepository(Ref ref) {
  return AuthRepository(dio: DioClient.instance.dio);
}

@Riverpod(keepAlive: true)
GoogleAuthService googleAuthService(Ref ref) => GoogleAuthService();

@Riverpod(keepAlive: true)
LocalStorage localStorage(Ref ref) => LocalStorage.instance;

@Riverpod(keepAlive: true)
SecureStorage secureStorage(Ref ref) => SecureStorage.instance;

@Riverpod(keepAlive: true)
AppRouter appRouter(Ref ref) => AppRouter.instance;

@Riverpod(keepAlive: true)
class AuthNotifier extends _$AuthNotifier {
  static const profileCacheTtl = Duration(days: 1);

  @override
  AuthState build() => const AuthState();

  Future<void> register({
    required String phone,
    required String password,
    required String displayName,
  }) async {
    await _authenticate(
      () => ref
          .read(authRepositoryProvider)
          .register(phone: phone, password: password, displayName: displayName),
    );
  }

  Future<void> login(String phone, String password) async {
    await _authenticate(
      () => ref
          .read(authRepositoryProvider)
          .login(phone: phone, password: password),
    );
  }

  Future<void> googleLogin(String idToken) async {
    await _authenticate(
      () => ref.read(authRepositoryProvider).googleLogin(idToken),
    );
  }

  Future<void> signInWithGoogle() async {
    state = state.copyWith(status: AuthStatus.loading, clearError: true);
    try {
      final idToken = await ref.read(googleAuthServiceProvider).getIdToken();
      await _authenticate(
        () => ref.read(authRepositoryProvider).googleLogin(idToken),
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
      final user = await ref.read(authRepositoryProvider).getCurrentUser();
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
          .read(authRepositoryProvider)
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

  Future<void> logout() async {
    final secureStorage = ref.read(secureStorageProvider);
    final refreshToken = await secureStorage.getRefreshToken();
    try {
      if (refreshToken != null && refreshToken.isNotEmpty) {
        await ref.read(authRepositoryProvider).logout(refreshToken);
      }
    } catch (_) {
      // Local logout must succeed even when the API is unavailable.
    } finally {
      await secureStorage.clearTokens();
      await ref.read(localStorageProvider).clearAll();
      state = const AuthState(status: AuthStatus.unauthenticated);
      ref.read(appRouterProvider).router.go(AppRoutes.login);
    }
  }

  Future<void> _authenticate(Future<AuthTokens> Function() request) async {
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
    } catch (error) {
      state = AuthState(
        status: AuthStatus.error,
        errorMessage: _errorMessage(error),
      );
    }
  }

  Future<void> _cacheUser(UserProfile user) {
    return ref
        .read(localStorageProvider)
        .setWithTtl(StorageKeys.userProfileJson, jsonEncode(user.toJson()));
  }

  Future<UserProfile?> _readCachedUser() async {
    final cached = await ref
        .read(localStorageProvider)
        .getWithTtl<String>(StorageKeys.userProfileJson, ttl: profileCacheTtl);
    if (cached == null) {
      return null;
    }

    try {
      return UserProfile.fromJson(jsonDecode(cached) as Map<String, dynamic>);
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
    return 'Đã xảy ra lỗi. Vui lòng thử lại.';
  }
}
