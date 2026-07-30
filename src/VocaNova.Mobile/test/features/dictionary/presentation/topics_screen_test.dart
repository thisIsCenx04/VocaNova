import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_search_repository.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/topics_screen.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

void main() {
  testWidgets('switches between global and personal topic counts', (
    tester,
  ) async {
    final repository = MockWordSearchRepository();
    when(() => repository.getTopics()).thenAnswer(
      (_) async => const [
        TopicSummary(topicId: 1, name: 'Fruit', wordCount: 42, icon: '🍎'),
      ],
    );
    when(() => repository.getPersonalTopics()).thenAnswer(
      (_) async => const [
        PersonalTopicSummary(
          topicId: 1,
          listId: 10,
          name: 'Fruit',
          wordCount: 2,
          containsWord: false,
          icon: '🍎',
        ),
      ],
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [wordSearchRepositoryProvider.overrideWithValue(repository)],
        child: MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: const TopicsScreen(),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('42 system words'), findsOneWidget);
    expect(
      find.text('Browse all words organized by the VocaNova team.'),
      findsOneWidget,
    );

    await tester.tap(find.byKey(const Key('topics-mode-personal')));
    await tester.pumpAndSettle();

    expect(find.text('2 personal words'), findsOneWidget);
    expect(
      find.text('Only words you saved appear here, ready for practice.'),
      findsOneWidget,
    );
    verify(() => repository.getPersonalTopics()).called(1);
  });
}

class MockWordSearchRepository extends Mock implements WordSearchRepository {}
