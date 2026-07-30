import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_theme.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/home/application/home_topics_provider.dart';
import 'package:vocanova_mobile/features/home/presentation/home_screen.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/notifications/application/notifications_notifier.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/wrong_words_notifier.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

import '../../../support/home_test_overrides.dart';

void main() {
  testWidgets('home renders dark palette, greeting, and quick actions', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          notificationsUnreadCountProvider.overrideWith((ref) async => 0),
          homeTopicsProvider.overrideWith((ref) async => const []),
          authProvider.overrideWith(FakeAuthNotifier.new),
          progressOverviewProvider.overrideWith(
            FakeProgressOverviewNotifier.new,
          ),
          listsProvider.overrideWith(FakeListsNotifier.new),
          wrongWordsProvider.overrideWith(FakeWrongWordsNotifier.new),
        ],
        child: MaterialApp(
          theme: AppTheme.dark(),
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: const HomeScreen(),
        ),
      ),
    );
    await tester.pumpAndSettle();

    final scaffold = tester.widget<Scaffold>(find.byType(Scaffold));
    final greeting = tester.widget<Text>(find.text('Hi, An'));
    expect(scaffold.backgroundColor, AppColors.background);
    expect(greeting.style?.color, AppColors.onSurface);
    expect(find.text('Search a word...'), findsOneWidget);
    expect(find.text('Quick actions'), findsOneWidget);
    expect(find.text('Quiz'), findsOneWidget);
    expect(find.text('Review'), findsOneWidget);
    expect(find.text('Topics'), findsOneWidget);
    expect(find.text('My lists'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
