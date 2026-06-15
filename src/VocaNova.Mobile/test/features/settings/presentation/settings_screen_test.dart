import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/features/settings/presentation/settings_screen.dart';

void main() {
  testWidgets('renders language, theme, notification stub and app version', (
    tester,
  ) async {
    await tester.pumpWidget(const MaterialApp(home: SettingsScreen()));
    await tester.pump();

    expect(find.byKey(const Key('language-dropdown')), findsOneWidget);
    expect(find.byKey(const Key('theme-switch')), findsOneWidget);
    expect(find.text('Tính năng đang được phát triển'), findsOneWidget);
    expect(find.text(appVersion), findsOneWidget);
  });
}
