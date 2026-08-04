import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('loads and persists locale and theme preferences', () async {
    SharedPreferences.setMockInitialValues({
      StorageKeys.appLocale: 'en',
      StorageKeys.appTheme: 'light',
    });
    final preferences = await SharedPreferences.getInstance();
    final storage = LocalStorage.create(preferences: preferences);
    final notifier = AppSettingsNotifier.create(storage);

    await notifier.load();
    expect(notifier.state.locale.languageCode, 'en');
    expect(notifier.state.themeMode, ThemeMode.light);

    await notifier.setLocale('vi');
    await notifier.setThemeMode(ThemeMode.dark);

    expect(preferences.getString(StorageKeys.appLocale), 'vi');
    expect(preferences.getString(StorageKeys.appTheme), 'dark');
    notifier.dispose();
  });
}
