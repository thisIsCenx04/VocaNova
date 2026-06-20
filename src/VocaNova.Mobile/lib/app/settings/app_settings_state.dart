import 'package:flutter/material.dart';

class AppSettingsState {
  const AppSettingsState({
    this.locale = const Locale('vi'),
    this.themeMode = ThemeMode.light,
    this.dailyReminder = true,
    this.streakAlert = true,
    this.reviewDueAlert = false,
    this.autoPlayPronunciation = true,
    this.soundEffects = true,
  });

  final Locale locale;
  final ThemeMode themeMode;
  final bool dailyReminder;
  final bool streakAlert;
  final bool reviewDueAlert;
  final bool autoPlayPronunciation;
  final bool soundEffects;

  AppSettingsState copyWith({
    Locale? locale,
    ThemeMode? themeMode,
    bool? dailyReminder,
    bool? streakAlert,
    bool? reviewDueAlert,
    bool? autoPlayPronunciation,
    bool? soundEffects,
  }) {
    return AppSettingsState(
      locale: locale ?? this.locale,
      themeMode: themeMode ?? this.themeMode,
      dailyReminder: dailyReminder ?? this.dailyReminder,
      streakAlert: streakAlert ?? this.streakAlert,
      reviewDueAlert: reviewDueAlert ?? this.reviewDueAlert,
      autoPlayPronunciation:
          autoPlayPronunciation ?? this.autoPlayPronunciation,
      soundEffects: soundEffects ?? this.soundEffects,
    );
  }
}
