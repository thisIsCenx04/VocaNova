import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/core/network/app_exception.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/data/auth_repository.dart';
import 'package:vocanova_mobile/features/auth/domain/onboarding_catalog.dart';
import 'package:vocanova_mobile/features/auth/domain/user_profile.dart';
import 'package:vocanova_mobile/features/auth/presentation/onboarding_screen.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_search_repository.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late MockAuthRepository repository;
  late MockWordSearchRepository wordSearchRepository;
  late MockLocalStorage localStorage;

  setUpAll(() {
    registerFallbackValue(const LearningProfile());
  });

  setUp(() async {
    localStorage = MockLocalStorage();
    repository = MockAuthRepository();
    wordSearchRepository = MockWordSearchRepository();
    when(
      () => localStorage.setWithTtl<String>(any(), any()),
    ).thenAnswer((_) async {});
    when(() => repository.getLearningProfileOptions()).thenAnswer(
      (_) async => const LearningProfileOptions(
        ageRanges: [],
        regions: [],
        occupations: [],
        educationLevels: [],
        learningPurposes: [
          OnboardingOption(id: 1, label: 'Giao tiếp hằng ngày'),
          OnboardingOption(id: 2, label: 'Công việc'),
        ],
      ),
    );
    when(() => wordSearchRepository.getTopics()).thenAnswer(
      (_) async => const [
        TopicSummary(topicId: 11, name: 'Travel', wordCount: 20),
        TopicSummary(topicId: 12, name: 'Business', wordCount: 30),
      ],
    );
    when(() => repository.selectOnboardingTopics(any())).thenAnswer(
      (_) async => 1,
    );
  });

  testWidgets('purpose step is single-select', (tester) async {
    await pumpOnboarding(
      tester,
      repository,
      wordSearchRepository,
      localStorage,
    );

    await tester.tap(find.byKey(const Key('onboarding-purpose-1')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('onboarding-purpose-2')));
    await tester.pump();

    expect(
      tester
          .widget<ChoiceChip>(find.byKey(const Key('onboarding-purpose-1')))
          .selected,
      isFalse,
    );
    expect(
      tester
          .widget<ChoiceChip>(find.byKey(const Key('onboarding-purpose-2')))
          .selected,
      isTrue,
    );
    expect(find.text('Step 1/2'), findsOneWidget);
  });

  testWidgets('topic step is multi-select', (tester) async {
    await pumpOnboarding(
      tester,
      repository,
      wordSearchRepository,
      localStorage,
    );

    await tester.tap(find.byKey(const Key('onboarding-next')));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('onboarding-topic-11')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('onboarding-topic-12')));
    await tester.pump();

    expect(find.text('Step 2/2'), findsOneWidget);
    expect(
      tester
          .widget<FilterChip>(find.byKey(const Key('onboarding-topic-11')))
          .selected,
      isTrue,
    );
    expect(
      tester
          .widget<FilterChip>(find.byKey(const Key('onboarding-topic-12')))
          .selected,
      isTrue,
    );
  });

  testWidgets('submits purpose and topic picks, then navigates home', (
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
    await pumpOnboarding(
      tester,
      repository,
      wordSearchRepository,
      localStorage,
    );

    await tester.tap(find.byKey(const Key('onboarding-purpose-2')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('onboarding-next')));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('onboarding-topic-11')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('onboarding-next')));
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));

    final capturedProfile =
        verify(
              () => repository.updateLearningProfile(captureAny()),
            ).captured.single
            as LearningProfile;
    expect(capturedProfile.learningPurposeId, 2);
    final capturedTopics =
        verify(
              () => repository.selectOnboardingTopics(captureAny()),
            ).captured.single
            as List<int>;
    expect(capturedTopics, [11]);
    expect(find.text('Home destination'), findsOneWidget);
  });

  testWidgets('skip navigates home without submitting profile', (tester) async {
    await pumpOnboarding(
      tester,
      repository,
      wordSearchRepository,
      localStorage,
    );

    await tester.tap(find.byKey(const Key('onboarding-skip')));
    await tester.pumpAndSettle();

    verifyNever(() => repository.updateLearningProfile(any()));
    verifyNever(() => repository.selectOnboardingTopics(any()));
    expect(find.text('Home destination'), findsOneWidget);
  });

  testWidgets('submit failure shows snackbar and stays on final step', (
    tester,
  ) async {
    when(
      () => repository.updateLearningProfile(any()),
    ).thenThrow(const AppException('Thiết lập học tập không hợp lệ.'));
    await pumpOnboarding(
      tester,
      repository,
      wordSearchRepository,
      localStorage,
    );

    await tester.tap(find.byKey(const Key('onboarding-next')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('onboarding-next')));
    await tester.pumpAndSettle();

    expect(find.text('Thiết lập học tập không hợp lệ.'), findsOneWidget);
    expect(find.text('Step 2/2'), findsOneWidget);
  });

  testWidgets('catalog failure offers a retry instead of blocking', (
    tester,
  ) async {
    when(
      () => repository.getLearningProfileOptions(),
    ).thenThrow(const AppException('offline'));
    await pumpOnboarding(
      tester,
      repository,
      wordSearchRepository,
      localStorage,
    );

    expect(
      find.text("Couldn't load the catalog. Please try again."),
      findsOne,
    );
    expect(find.text('Retry'), findsOneWidget);
  });
}

Future<void> pumpOnboarding(
  WidgetTester tester,
  AuthRepository repository,
  WordSearchRepository wordSearchRepository,
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
        wordSearchRepositoryProvider.overrideWithValue(wordSearchRepository),
        localStorageProvider.overrideWithValue(localStorage),
      ],
      child: MaterialApp.router(
        routerConfig: router,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
      ),
    ),
  );
  await tester.pumpAndSettle();
}

class MockAuthRepository extends Mock implements AuthRepository {}

class MockWordSearchRepository extends Mock implements WordSearchRepository {}

class MockLocalStorage extends Mock implements LocalStorage {}
