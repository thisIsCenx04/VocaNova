import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_theme.dart';
import 'package:vocanova_mobile/features/home/presentation/home_screen.dart';
import 'package:vocanova_mobile/features/notifications/application/notifications_notifier.dart';

void main() {
  testWidgets('home applies the dark palette without layout overflow', (
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
        ],
        child: MaterialApp(theme: AppTheme.dark(), home: const HomeScreen()),
      ),
    );
    await tester.pumpAndSettle();

    final scaffold = tester.widget<Scaffold>(find.byType(Scaffold));
    final greeting = tester.widget<Text>(find.text('Hi, An'));
    expect(scaffold.backgroundColor, AppColors.background);
    expect(greeting.style?.color, AppColors.onSurface);
    expect(tester.takeException(), isNull);
  });
}
