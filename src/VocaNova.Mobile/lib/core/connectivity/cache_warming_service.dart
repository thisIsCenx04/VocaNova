import 'dart:convert';

import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/lists/data/dtos/user_list_dto.dart';
import 'package:vocanova_mobile/features/lists/data/services/lists_api_service.dart';
import 'package:vocanova_mobile/features/progress/data/dtos/progress_summary_dto.dart';
import 'package:vocanova_mobile/features/progress/data/services/progress_api_service.dart';

class CacheWarmingService {
  const CacheWarmingService({
    required ListsApiService listsApiService,
    required ProgressApiService progressApiService,
    required LocalStorage storage,
  }) : _listsApiService = listsApiService,
       _progressApiService = progressApiService,
       _storage = storage;

  final ListsApiService _listsApiService;
  final ProgressApiService _progressApiService;
  final LocalStorage _storage;

  Future<void> warm() async {
    await Future.wait([_warmLists(), _warmProgress()]);
  }

  Future<void> _warmLists() async {
    try {
      final lists = await _listsApiService.getLists();
      await _storage.set(
        StorageKeys.listsCacheJson,
        jsonEncode(
          lists.map((list) => UserListDto.fromDomain(list).toJson()).toList(),
        ),
      );
    } catch (_) {
      // Cache warming is best-effort and must not block login.
    }
  }

  Future<void> _warmProgress() async {
    try {
      final summary = await _progressApiService.getSummary();
      await _storage.setWithTtl(
        StorageKeys.progressSummaryJson,
        jsonEncode(ProgressSummaryDto.fromDomain(summary).toJson()),
      );
    } catch (_) {
      // Cache warming is best-effort and must not block login.
    }
  }
}
