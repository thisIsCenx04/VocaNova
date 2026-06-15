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
      themeMode: theme == 'light' ? ThemeMode.light : ThemeMode.dark,
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
    final value = themeMode == ThemeMode.light ? 'light' : 'dark';
    await _storage.set(StorageKeys.appTheme, value);
    _state = _state.copyWith(themeMode: themeMode);
    notifyListeners();
  }
}
