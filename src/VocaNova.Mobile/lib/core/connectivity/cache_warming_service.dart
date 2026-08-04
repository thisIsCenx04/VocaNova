import 'dart:convert';

import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/lists/data/lists_repository.dart';
import 'package:vocanova_mobile/features/progress/data/progress_repository.dart';

class CacheWarmingService {
  const CacheWarmingService({
    required ListsRepository listsRepository,
    required ProgressRepository progressRepository,
    required LocalStorage storage,
  }) : _listsRepository = listsRepository,
       _progressRepository = progressRepository,
       _storage = storage;

  final ListsRepository _listsRepository;
  final ProgressRepository _progressRepository;
  final LocalStorage _storage;

  Future<void> warm() async {
    await Future.wait([_warmLists(), _warmProgress()]);
  }

  Future<void> _warmLists() async {
    try {
      final lists = await _listsRepository.getLists();
      await _storage.set(
        StorageKeys.listsCacheJson,
        jsonEncode(lists.map((list) => list.toJson()).toList()),
      );
    } catch (_) {
      // Cache warming is best-effort and must not block login.
    }
  }

  Future<void> _warmProgress() async {
    try {
      final summary = await _progressRepository.getSummary();
      await _storage.setWithTtl(
        StorageKeys.progressSummaryJson,
        jsonEncode(summary.toJson()),
      );
    } catch (_) {
      // Cache warming is best-effort and must not block login.
    }
  }
}
