import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';
import 'package:vocanova_mobile/app/theme/app_theme.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('dark theme uses the VocaNova dark palette', () {
    final theme = AppTheme.dark();

    expect(theme.brightness, Brightness.dark);
    expect(theme.colorScheme.primary, AppColors.primary);
    expect(theme.scaffoldBackgroundColor, AppColors.background);
    expect(theme.colorScheme.surface, AppColors.surface);
    expect(theme.colorScheme.error, AppColors.error);
  });

  test('light theme and named text styles are available', () {
    final theme = AppTheme.light();

    expect(theme.brightness, Brightness.light);
    expect(theme.colorScheme.primary, AppColors.primary);
    expect(AppTextStyles.heading.fontFamily, AppTextStyles.fontFamily);
    expect(AppTextStyles.body.fontFamily, AppTextStyles.fontFamily);
    expect(AppTextStyles.caption.fontFamily, AppTextStyles.fontFamily);
    expect(AppTextStyles.label.fontFamily, AppTextStyles.fontFamily);
  });
}
