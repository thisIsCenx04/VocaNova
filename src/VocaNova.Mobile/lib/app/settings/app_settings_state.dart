import 'package:flutter/material.dart';

class AppSettingsState {
  const AppSettingsState({
    this.locale = const Locale('vi'),
    this.themeMode = ThemeMode.dark,
  });

  final Locale locale;
  final ThemeMode themeMode;

  AppSettingsState copyWith({Locale? locale, ThemeMode? themeMode}) {
    return AppSettingsState(
      locale: locale ?? this.locale,
      themeMode: themeMode ?? this.themeMode,
    );
  }
}
