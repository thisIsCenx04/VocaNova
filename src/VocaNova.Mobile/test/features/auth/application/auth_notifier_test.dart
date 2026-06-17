import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/app/router/app_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/core/connectivity/cache_warming_service.dart';
import 'package:vocanova_mobile/core/network/app_exception.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/secure_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/data/auth_repository.dart';
import 'package:vocanova_mobile/features/auth/domain/auth_state.dart';
import 'package:vocanova_mobile/features/auth/domain/user_profile.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  const user = UserProfile(
    userId: 7,
    phone: '0901234567',
    displayName: 'Nhut',
    role: 'user',
    status: 'active',
  );
  const tokens = AuthTokens(
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    expiresIn: 900,
    tokenType: 'Bearer',
  );

  late MockAuthRepository repository;
  late MockSecureStorage secureStorage;
  late MockAppRouter appRouter;
  late MockGoRouter goRouter;
  late MockCacheWarmingService cacheWarmingService;
  late LocalStorage localStorage;
  late SharedPreferences preferences;
  late ProviderContainer container;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    preferences = await SharedPreferences.getInstance();
    localStorage = LocalStorage.create(preferences: preferences);
    repository = MockAuthRepository();
    secureStorage = MockSecureStorage();
    appRouter = MockAppRouter();
    goRouter = MockGoRouter();
    cacheWarmingService = MockCacheWarmingService();
    when(() => appRouter.router).thenReturn(goRouter);
    when(() => cacheWarmingService.warm()).thenAnswer((_) async {});

    container = ProviderContainer(
      overrides: [
        authRepositoryProvider.overrideWithValue(repository),
        localStorageProvider.overrideWithValue(localStorage),
        secureStorageProvider.overrideWithValue(secureStorage),
        appRouterProvider.overrideWithValue(appRouter),
        cacheWarmingServiceProvider.overrideWithValue(cacheWarmingService),
      ],
    );
  });

  tearDown(() => container.dispose());

  test(
    'login saves tokens, loads profile, and caches it for one day',
    () async {
      when(
        () => repository.login(phone: '0901234567', password: 'Password1'),
      ).thenAnswer((_) async => tokens);
      when(
        () => secureStorage.saveTokens(
          accessToken: tokens.accessToken,
          refreshToken: tokens.refreshToken,
        ),
      ).thenAnswer((_) async {});
      when(
        () => secureStorage.getAccessToken(),
      ).thenAnswer((_) async => tokens.accessToken);
      when(() => repository.getCurrentUser()).thenAnswer((_) async => user);

      await container
          .read(authProvider.notifier)
          .login('0901234567', 'Password1');

      final state = container.read(authProvider);
      final cachedJson = await localStorage.getWithTtl<String>(
        StorageKeys.userProfileJson,
        ttl: AuthNotifier.profileCacheTtl,
      );
      expect(state.status, AuthStatus.authenticated);
      expect(state.user?.displayName, 'Nhut');
      expect(jsonDecode(cachedJson!)['user_id'], 7);
      verify(
        () => secureStorage.saveTokens(
          accessToken: tokens.accessToken,
          refreshToken: tokens.refreshToken,
        ),
      ).called(1);
      verify(() => cacheWarmingService.warm()).called(1);
    },
  );

  test('register authenticates with the backend register contract', () async {
    when(
      () => repository.register(
        phone: '0901234567',
        password: 'Password1',
        displayName: 'Nhut',
        otpCode: '123456',
      ),
    ).thenAnswer((_) async => tokens);
    when(
      () => secureStorage.saveTokens(
        accessToken: tokens.accessToken,
        refreshToken: tokens.refreshToken,
      ),
    ).thenAnswer((_) async {});
    when(
      () => secureStorage.getAccessToken(),
    ).thenAnswer((_) async => tokens.accessToken);
    when(() => repository.getCurrentUser()).thenAnswer((_) async => user);

    await container
        .read(authProvider.notifier)
        .register(
          phone: '0901234567',
          password: 'Password1',
          displayName: 'Nhut',
          otpCode: '123456',
        );

    expect(container.read(authProvider).status, AuthStatus.authenticated);
    verify(
      () => repository.register(
        phone: '0901234567',
        password: 'Password1',
        displayName: 'Nhut',
        otpCode: '123456',
      ),
    ).called(1);
  });

  test('loadCurrentUser uses valid one-day cache when API fails', () async {
    await localStorage.setWithTtl(
      StorageKeys.userProfileJson,
      jsonEncode(user.toJson()),
    );
    when(
      () => secureStorage.getAccessToken(),
    ).thenAnswer((_) async => tokens.accessToken);
    when(
      () => repository.getCurrentUser(),
    ).thenThrow(const AppException('Không thể kết nối đến máy chủ.'));

    await container.read(authProvider.notifier).loadCurrentUser();

    final state = container.read(authProvider);
    expect(state.status, AuthStatus.authenticated);
    expect(state.user?.userId, 7);
  });

  test('updateLearningProfile updates state and cached profile', () async {
    const learningProfile = LearningProfile(
      ageRangeId: 1,
      regionId: 2,
      occupationId: 3,
      educationLevelId: 4,
      learningPurposeId: 5,
    );
    const updatedUser = UserProfile(
      userId: 7,
      phone: '0901234567',
      displayName: 'Nhut',
      role: 'user',
      status: 'active',
      learningProfile: learningProfile,
    );
    when(
      () => repository.updateLearningProfile(learningProfile),
    ).thenAnswer((_) async => updatedUser);

    final success = await container
        .read(authProvider.notifier)
        .updateLearningProfile(learningProfile);

    final cachedJson = await localStorage.getWithTtl<String>(
      StorageKeys.userProfileJson,
      ttl: AuthNotifier.profileCacheTtl,
    );
    expect(success, isTrue);
    expect(container.read(authProvider).user?.learningProfile?.occupationId, 3);
    expect(
      jsonDecode(cachedJson!)['learning_profile']['learning_purpose_id'],
      5,
    );
  });

  test('updateProfile updates state and cached display name', () async {
    const updatedUser = UserProfile(
      userId: 7,
      phone: '0901234567',
      displayName: 'Nhut Updated',
      role: 'user',
      status: 'active',
    );
    when(
      () => repository.updateProfile(
        displayName: 'Nhut Updated',
        avatarUrl: null,
      ),
    ).thenAnswer((_) async => updatedUser);

    final success = await container
        .read(authProvider.notifier)
        .updateProfile(displayName: 'Nhut Updated');

    final cachedJson = await localStorage.getWithTtl<String>(
      StorageKeys.userProfileJson,
      ttl: AuthNotifier.profileCacheTtl,
    );
    expect(success, isTrue);
    expect(container.read(authProvider).user?.displayName, 'Nhut Updated');
    expect(jsonDecode(cachedJson!)['display_name'], 'Nhut Updated');
  });

  test('login exposes Vietnamese AppException from Dio interceptor', () async {
    when(
      () => repository.login(phone: '0901234567', password: 'wrong'),
    ).thenThrow(
      DioException(
        requestOptions: RequestOptions(path: '/api/auth/login'),
        error: const AppException('Số điện thoại hoặc mật khẩu không đúng.'),
      ),
    );

    await container.read(authProvider.notifier).login('0901234567', 'wrong');

    final state = container.read(authProvider);
    expect(state.status, AuthStatus.error);
    expect(state.errorMessage, 'Số điện thoại hoặc mật khẩu không đúng.');
  });

  test('logout clears all storage and navigates even when API fails', () async {
    await localStorage.set(StorageKeys.appLocale, 'vi');
    when(
      () => secureStorage.getRefreshToken(),
    ).thenAnswer((_) async => tokens.refreshToken);
    when(
      () => repository.logout(tokens.refreshToken),
    ).thenThrow(const AppException('Máy chủ đang gặp sự cố.'));
    when(() => secureStorage.clearTokens()).thenAnswer((_) async {});
    when(() => goRouter.go(AppRoutes.login)).thenReturn(null);

    await container.read(authProvider.notifier).logout();

    expect(container.read(authProvider).status, AuthStatus.unauthenticated);
    expect(preferences.getKeys(), isEmpty);
    verify(() => secureStorage.clearTokens()).called(1);
    verify(() => goRouter.go(AppRoutes.login)).called(1);
  });
}

class MockAuthRepository extends Mock implements AuthRepository {}

class MockSecureStorage extends Mock implements SecureStorage {}

class MockAppRouter extends Mock implements AppRouter {}

class MockGoRouter extends Mock implements GoRouter {}

class MockCacheWarmingService extends Mock implements CacheWarmingService {}
