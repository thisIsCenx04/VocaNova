import 'package:shared_preferences/shared_preferences.dart';

typedef Clock = DateTime Function();

class LocalStorage {
  LocalStorage._({
    required Future<SharedPreferences> preferences,
    Clock clock = DateTime.now,
  }) : _preferences = preferences,
       _clock = clock;

  static final LocalStorage instance = LocalStorage._(
    preferences: SharedPreferences.getInstance(),
  );

  final Future<SharedPreferences> _preferences;
  final Clock _clock;

  static LocalStorage create({
    required SharedPreferences preferences,
    Clock clock = DateTime.now,
  }) {
    return LocalStorage._(preferences: Future.value(preferences), clock: clock);
  }

  Future<T?> get<T>(String key) async {
    final preferences = await _preferences;
    return preferences.get(key) as T?;
  }

  Future<T?> getWithTtl<T>(String key, {required Duration ttl}) async {
    final preferences = await _preferences;
    final savedAt = preferences.getInt(_savedAtKey(key));
    if (savedAt == null) {
      return null;
    }

    final age = _clock().difference(
      DateTime.fromMillisecondsSinceEpoch(savedAt),
    );
    if (age.isNegative || age >= ttl) {
      await remove(key);
      return null;
    }

    return preferences.get(key) as T?;
  }

  Future<void> set<T>(String key, T value) async {
    final preferences = await _preferences;
    await _setValue(preferences, key, value);
  }

  Future<void> setWithTtl<T>(String key, T value) async {
    final preferences = await _preferences;
    await _setValue(preferences, key, value);
    await preferences.setInt(_savedAtKey(key), _clock().millisecondsSinceEpoch);
  }

  Future<void> remove(String key) async {
    final preferences = await _preferences;
    await Future.wait([
      preferences.remove(key),
      preferences.remove(_savedAtKey(key)),
    ]);
  }

  Future<void> clearAll() async {
    final preferences = await _preferences;
    await preferences.clear();
  }

  Future<void> _setValue<T>(
    SharedPreferences preferences,
    String key,
    T value,
  ) async {
    final result = switch (value) {
      String value => preferences.setString(key, value),
      bool value => preferences.setBool(key, value),
      int value => preferences.setInt(key, value),
      double value => preferences.setDouble(key, value),
      List<String> value => preferences.setStringList(key, value),
      _ => throw UnsupportedError(
        'LocalStorage only supports String, bool, int, double, and List<String>.',
      ),
    };

    if (!await result) {
      throw StateError('Could not save local storage key "$key".');
    }
  }

  String _savedAtKey(String key) => '${key}_saved_at';
}
