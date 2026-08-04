import 'dart:convert';

import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_state.dart';
import 'package:vocanova_mobile/features/progress/data/progress_repository.dart';
import 'package:vocanova_mobile/features/progress/domain/progress_summary.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

part 'progress_overview_notifier.g.dart';

@Riverpod(keepAlive: true)
ProgressRepository progressRepository(Ref ref) =>
    ProgressRepository(dio: DioClient.instance.dio);

@Riverpod(keepAlive: true)
LocalStorage progressLocalStorage(Ref ref) => LocalStorage.instance;

@riverpod
class ProgressOverviewNotifier extends _$ProgressOverviewNotifier {
  static const cacheTtl = Duration(minutes: 15);

  @override
  ProgressOverviewState build() => const ProgressOverviewState();

  Future<void> load() async {
    final storage = ref.read(progressLocalStorageProvider);
    final cached = _decode(
      await storage.getWithTtl<String>(
        StorageKeys.progressSummaryJson,
        ttl: cacheTtl,
      ),
    );
    final online = await ref.read(connectivityServiceProvider).isOnline;
    final l10n = lookupAppLocalizations(
      AppSettingsNotifier.instance.state.locale,
    );
    if (!online) {
      state = ProgressOverviewState(
        summary: cached,
        isLoading: false,
        isOffline: true,
        errorMessage: cached == null ? l10n.progressNoCachedDataError : null,
      );
      return;
    }

    try {
      final summary = await ref.read(progressRepositoryProvider).getSummary();
      state = ProgressOverviewState(summary: summary, isLoading: false);
      await storage.setWithTtl(
        StorageKeys.progressSummaryJson,
        jsonEncode(summary.toJson()),
      );
    } catch (_) {
      state = ProgressOverviewState(
        summary: cached,
        isLoading: false,
        isOffline: cached != null,
        errorMessage: cached == null ? l10n.progressLoadOverviewError : null,
      );
    }
  }

  ProgressSummary? _decode(String? source) {
    try {
      final json = jsonDecode(source ?? '');
      return json is Map<String, dynamic>
          ? ProgressSummary.fromJson(json)
          : null;
    } on FormatException {
      return null;
    }
  }
}
