import 'package:flutter/material.dart';
import 'package:vocanova_mobile/app/settings/app_settings_state.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';

class AppSettingsNotifier extends ChangeNotifier {
  AppSettingsNotifier._(this._storage) {
    load();
  }

  static final AppSettingsNotifier instance = AppSettingsNotifier._(
    LocalStorage.instance,
  );

  @visibleForTesting
  static AppSettingsNotifier create(LocalStorage storage) {
    return AppSettingsNotifier._(storage);
  }

  final LocalStorage _storage;
  AppSettingsState _state = const AppSettingsState();

  AppSettingsState get state => _state;

  Future<void> load() async {
    final locale = await _storage.get<String>(StorageKeys.appLocale);
    final theme = await _storage.get<String>(StorageKeys.appTheme);
    _state = AppSettingsState(
      locale: Locale(locale == 'en' ? 'en' : 'vi'),
      themeMode: switch (theme) {
        'dark' => ThemeMode.dark,
        'system' => ThemeMode.system,
        _ => ThemeMode.light,
      },
      dailyReminder:
          await _storage.get<bool>(StorageKeys.dailyReminder) ?? true,
      streakAlert: await _storage.get<bool>(StorageKeys.streakAlert) ?? true,
      reviewDueAlert:
          await _storage.get<bool>(StorageKeys.reviewDueAlert) ?? false,
      autoPlayPronunciation:
          await _storage.get<bool>(StorageKeys.autoPlayPronunciation) ?? true,
      soundEffects: await _storage.get<bool>(StorageKeys.soundEffects) ?? true,
    );
    notifyListeners();
  }

  Future<void> setLocale(String languageCode) async {
    final normalized = languageCode == 'en' ? 'en' : 'vi';
    await _storage.set(StorageKeys.appLocale, normalized);
    _state = _state.copyWith(locale: Locale(normalized));
    notifyListeners();
  }

  Future<void> setThemeMode(ThemeMode themeMode) async {
    final value = switch (themeMode) {
      ThemeMode.dark => 'dark',
      ThemeMode.system => 'system',
      ThemeMode.light => 'light',
    };
    await _storage.set(StorageKeys.appTheme, value);
    _state = _state.copyWith(themeMode: themeMode);
    notifyListeners();
  }

  Future<void> setDailyReminder(bool value) => _setBool(
    key: StorageKeys.dailyReminder,
    value: value,
    update: () => _state = _state.copyWith(dailyReminder: value),
  );

  Future<void> setStreakAlert(bool value) => _setBool(
    key: StorageKeys.streakAlert,
    value: value,
    update: () => _state = _state.copyWith(streakAlert: value),
  );

  Future<void> setReviewDueAlert(bool value) => _setBool(
    key: StorageKeys.reviewDueAlert,
    value: value,
    update: () => _state = _state.copyWith(reviewDueAlert: value),
  );

  Future<void> setAutoPlayPronunciation(bool value) => _setBool(
    key: StorageKeys.autoPlayPronunciation,
    value: value,
    update: () => _state = _state.copyWith(autoPlayPronunciation: value),
  );

  Future<void> setSoundEffects(bool value) => _setBool(
    key: StorageKeys.soundEffects,
    value: value,
    update: () => _state = _state.copyWith(soundEffects: value),
  );

  Future<void> _setBool({
    required String key,
    required bool value,
    required VoidCallback update,
  }) async {
    await _storage.set(key, value);
    update();
    notifyListeners();
  }
}
