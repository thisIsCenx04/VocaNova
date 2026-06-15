import 'dart:convert';

import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_state.dart';
import 'package:vocanova_mobile/features/progress/data/progress_repository.dart';
import 'package:vocanova_mobile/features/progress/domain/progress_summary.dart';

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
    if (!online) {
      state = ProgressOverviewState(
        summary: cached,
        isLoading: false,
        isOffline: true,
        errorMessage: cached == null
            ? 'Không có dữ liệu tiến độ đã lưu.'
            : null,
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
        errorMessage: cached == null
            ? 'Không thể tải tổng quan tiến độ.'
            : null,
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
