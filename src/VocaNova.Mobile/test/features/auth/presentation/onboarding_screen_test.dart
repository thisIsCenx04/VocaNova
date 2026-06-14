import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/core/network/app_exception.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/data/auth_repository.dart';
import 'package:vocanova_mobile/features/auth/domain/user_profile.dart';
import 'package:vocanova_mobile/features/auth/presentation/onboarding_screen.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late MockAuthRepository repository;
  late MockLocalStorage localStorage;

  setUpAll(() {
    registerFallbackValue(const LearningProfile());
  });

  setUp(() async {
    localStorage = MockLocalStorage();
    repository = MockAuthRepository();
    when(
      () => localStorage.setWithTtl<String>(any(), any()),
    ).thenAnswer((_) async {});
  });

  testWidgets('onboarding uses single-select chips and five steps', (
    tester,
  ) async {
    await pumpOnboarding(tester, repository, localStorage);

    await tester.tap(find.byKey(const Key('onboarding-0-1')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('onboarding-0-2')));
    await tester.pump();

    expect(
      tester
          .widget<ChoiceChip>(find.byKey(const Key('onboarding-0-1')))
          .selected,
      isFalse,
    );
    expect(
      tester
          .widget<ChoiceChip>(find.byKey(const Key('onboarding-0-2')))
          .selected,
      isTrue,
    );

    for (var step = 0; step < 4; step++) {
      await tester.tap(find.byKey(const Key('onboarding-next')));
      await tester.pumpAndSettle();
    }

    expect(find.text('Bước 5/5'), findsOneWidget);
    expect(find.text('Mục tiêu học từ vựng của bạn?'), findsOneWidget);
  });

  testWidgets('onboarding submits selected profile and navigates home', (
    tester,
  ) async {
    when(() => repository.updateLearningProfile(any())).thenAnswer((
      invocation,
    ) async {
      final profile = invocation.positionalArguments.first as LearningProfile;
      return UserProfile(
        userId: 7,
        displayName: 'Nhut',
        role: 'user',
        status: 'active',
        learningProfile: profile,
      );
    });
    await pumpOnboarding(tester, repository, localStorage);

    for (var step = 0; step < 5; step++) {
      await tester.tap(find.byKey(Key('onboarding-$step-1')));
      await tester.pump();
      await tester.tap(find.byKey(const Key('onboarding-next')));
      if (step < 4) {
        await tester.pumpAndSettle();
      } else {
        await tester.pump();
        await tester.pump(const Duration(seconds: 1));
      }
    }

    final captured =
        verify(
              () => repository.updateLearningProfile(captureAny()),
            ).captured.single
            as LearningProfile;
    expect(captured.ageRangeId, 1);
    expect(captured.learningPurposeId, 1);
    expect(find.text('Home destination'), findsOneWidget);
  });

  testWidgets('skip navigates home without submitting profile', (tester) async {
    await pumpOnboarding(tester, repository, localStorage);

    await tester.tap(find.byKey(const Key('onboarding-skip')));
    await tester.pumpAndSettle();

    verifyNever(() => repository.updateLearningProfile(any()));
    expect(find.text('Home destination'), findsOneWidget);
  });

  testWidgets('submit failure shows snackbar and stays on final step', (
    tester,
  ) async {
    when(
      () => repository.updateLearningProfile(any()),
    ).thenThrow(const AppException('Thiết lập học tập không hợp lệ.'));
    await pumpOnboarding(tester, repository, localStorage);

    for (var step = 0; step < 5; step++) {
      await tester.tap(find.byKey(const Key('onboarding-next')));
      await tester.pumpAndSettle();
    }

    expect(find.text('Thiết lập học tập không hợp lệ.'), findsOneWidget);
    expect(find.text('Bước 5/5'), findsOneWidget);
  });
}

Future<void> pumpOnboarding(
  WidgetTester tester,
  AuthRepository repository,
  LocalStorage localStorage,
) async {
  final router = GoRouter(
    initialLocation: '/onboarding',
    routes: [
      GoRoute(path: '/onboarding', builder: (_, _) => const OnboardingScreen()),
      GoRoute(
        path: '/home',
        builder: (_, _) => const Scaffold(body: Text('Home destination')),
      ),
    ],
  );
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        authRepositoryProvider.overrideWithValue(repository),
        localStorageProvider.overrideWithValue(localStorage),
      ],
      child: MaterialApp.router(routerConfig: router),
    ),
  );
  await tester.pumpAndSettle();
}

class MockAuthRepository extends Mock implements AuthRepository {}

class MockLocalStorage extends Mock implements LocalStorage {}
