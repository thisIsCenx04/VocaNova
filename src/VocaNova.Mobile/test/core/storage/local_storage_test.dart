import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late DateTime now;
  late SharedPreferences preferences;
  late LocalStorage storage;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    preferences = await SharedPreferences.getInstance();
    now = DateTime(2026, 6, 13, 8);
    storage = LocalStorage.create(preferences: preferences, clock: () => now);
  });

  test('TTL not expired returns stored value', () async {
    await storage.setWithTtl(StorageKeys.userProfileJson, '{"id":1}');
    now = now.add(const Duration(hours: 23));

    final value = await storage.getWithTtl<String>(
      StorageKeys.userProfileJson,
      ttl: const Duration(days: 1),
    );

    expect(value, '{"id":1}');
    expect(
      preferences.getInt('${StorageKeys.userProfileJson}_saved_at'),
      isNotNull,
    );
  });

  test('TTL expired returns null and removes value with timestamp', () async {
    await storage.setWithTtl(StorageKeys.progressSummaryJson, '{"streak":3}');
    now = now.add(const Duration(minutes: 16));

    final value = await storage.getWithTtl<String>(
      StorageKeys.progressSummaryJson,
      ttl: const Duration(minutes: 15),
    );

    expect(value, isNull);
    expect(preferences.containsKey(StorageKeys.progressSummaryJson), isFalse);
    expect(
      preferences.containsKey('${StorageKeys.progressSummaryJson}_saved_at'),
      isFalse,
    );
  });

  test('get and set support SharedPreferences value types', () async {
    await storage.set(StorageKeys.appLocale, 'vi');
    await storage.set(StorageKeys.appTheme, 'dark');
    await storage.set('notifications_enabled', true);
    await storage.set('daily_goal', 10);
    await storage.set('accuracy', 0.85);
    await storage.set('recent_words', ['hello', 'world']);

    expect(await storage.get<String>(StorageKeys.appLocale), 'vi');
    expect(await storage.get<String>(StorageKeys.appTheme), 'dark');
    expect(await storage.get<bool>('notifications_enabled'), isTrue);
    expect(await storage.get<int>('daily_goal'), 10);
    expect(await storage.get<double>('accuracy'), 0.85);
    expect(await storage.get<List<String>>('recent_words'), ['hello', 'world']);
    expect(StorageKeys.wordCacheJson(42), 'word_cache_42_json');
  });

  test('remove deletes value and TTL metadata', () async {
    await storage.setWithTtl(StorageKeys.listsCacheJson, '[]');

    await storage.remove(StorageKeys.listsCacheJson);

    expect(await storage.get<String>(StorageKeys.listsCacheJson), isNull);
    expect(
      preferences.containsKey('${StorageKeys.listsCacheJson}_saved_at'),
      isFalse,
    );
  });

  test('clearAll removes every stored value', () async {
    await storage.set(StorageKeys.appLocale, 'vi');
    await storage.setWithTtl(StorageKeys.searchHistoryJson, '[]');

    await storage.clearAll();

    expect(preferences.getKeys(), isEmpty);
  });
}
