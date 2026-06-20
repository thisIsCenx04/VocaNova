import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/features/settings/presentation/settings_screen.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('matches the Figma sections and persists interactive settings', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    SharedPreferences.setMockInitialValues({});
    await AppSettingsNotifier.instance.setThemeMode(ThemeMode.light);
    await AppSettingsNotifier.instance.setLocale('vi');
    await tester.pumpWidget(
      const ProviderScope(child: MaterialApp(home: SettingsScreen())),
    );
    await tester.pump();

    expect(find.text('Settings'), findsOneWidget);
    expect(find.text('APPEARANCE'), findsOneWidget);
    expect(find.text('LANGUAGE'), findsOneWidget);
    expect(find.byKey(const Key('language-dropdown')), findsOneWidget);
    expect(find.byKey(const Key('theme-switch')), findsOneWidget);
    expect(find.byKey(const Key('daily-reminder-switch')), findsOneWidget);

    await tester.tap(find.byKey(const Key('theme-switch')));
    await tester.pumpAndSettle();
    expect(AppSettingsNotifier.instance.state.themeMode, ThemeMode.dark);

    await tester.tap(find.text('EN'));
    await tester.pumpAndSettle();
    expect(AppSettingsNotifier.instance.state.locale.languageCode, 'en');

    await tester.drag(
      find.byKey(const Key('settings-list')),
      const Offset(0, -700),
    );
    await tester.pumpAndSettle();
    expect(find.text('Privacy policy'), findsOneWidget);
    expect(find.text('Delete account'), findsOneWidget);
    expect(find.text('VocaNova v$appVersion'), findsOneWidget);
  });
}
