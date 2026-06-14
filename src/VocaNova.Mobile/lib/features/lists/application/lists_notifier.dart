import 'dart:convert';

import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/lists/application/lists_state.dart';
import 'package:vocanova_mobile/features/lists/data/lists_repository.dart';
import 'package:vocanova_mobile/features/lists/domain/user_list.dart';

part 'lists_notifier.g.dart';

@Riverpod(keepAlive: true)
ListsRepository listsRepository(Ref ref) =>
    ListsRepository(dio: DioClient.instance.dio);

@Riverpod(keepAlive: true)
LocalStorage listsLocalStorage(Ref ref) => LocalStorage.instance;

@Riverpod(keepAlive: true)
class ListsNotifier extends _$ListsNotifier {
  @override
  ListsState build() => const ListsState();

  Future<void> load() async {
    final storage = ref.read(listsLocalStorageProvider);
    final cached = _decode(
      await storage.get<String>(StorageKeys.listsCacheJson),
    );
    final online = await ref.read(connectivityServiceProvider).isOnline;
    if (!online) {
      state = ListsState(lists: cached, isLoading: false, isOffline: true);
      return;
    }

    try {
      final lists = await ref.read(listsRepositoryProvider).getLists();
      state = ListsState(lists: lists, isLoading: false);
      await _cache(lists);
    } catch (_) {
      state = ListsState(
        lists: cached,
        isLoading: false,
        isOffline: cached.isNotEmpty,
        errorMessage: cached.isEmpty ? 'Không thể tải danh sách từ.' : null,
      );
    }
  }

  Future<bool> create(String name) async {
    state = state.copyWith(isMutating: true, clearError: true);
    try {
      final created = await ref
          .read(listsRepositoryProvider)
          .create(name.trim());
      final lists = [created, ...state.lists];
      state = state.copyWith(lists: lists, isMutating: false);
      await _cache(lists);
      return true;
    } catch (_) {
      state = state.copyWith(
        isMutating: false,
        errorMessage: 'Không thể tạo danh sách. Tên có thể đã tồn tại.',
      );
      return false;
    }
  }

  Future<bool> rename({required int listId, required String name}) async {
    state = state.copyWith(isMutating: true, clearError: true);
    try {
      final updated = await ref
          .read(listsRepositoryProvider)
          .rename(listId: listId, name: name.trim());
      final lists = [
        for (final list in state.lists)
          if (list.listId == listId) updated else list,
      ];
      state = state.copyWith(lists: lists, isMutating: false);
      await _cache(lists);
      return true;
    } catch (_) {
      state = state.copyWith(
        isMutating: false,
        errorMessage: 'Không thể đổi tên danh sách.',
      );
      return false;
    }
  }

  Future<bool> delete(int listId) async {
    final index = state.lists.indexWhere((list) => list.listId == listId);
    if (index < 0) return false;
    final removed = state.lists[index];
    final optimistic = [...state.lists]..removeAt(index);
    state = state.copyWith(lists: optimistic, clearError: true);
    try {
      await ref.read(listsRepositoryProvider).delete(listId);
      await _cache(optimistic);
      return true;
    } catch (_) {
      final rollback = [...state.lists]..insert(index, removed);
      state = state.copyWith(
        lists: rollback,
        errorMessage: 'Không thể xóa danh sách.',
      );
      return false;
    }
  }

  Future<void> _cache(List<UserList> lists) => ref
      .read(listsLocalStorageProvider)
      .set(
        StorageKeys.listsCacheJson,
        jsonEncode(lists.map((list) => list.toJson()).toList()),
      );

  List<UserList> _decode(String? source) {
    try {
      return (jsonDecode(source ?? '[]') as List<dynamic>)
          .whereType<Map<String, dynamic>>()
          .map(UserList.fromJson)
          .toList(growable: false);
    } on FormatException {
      return const [];
    }
  }
}
