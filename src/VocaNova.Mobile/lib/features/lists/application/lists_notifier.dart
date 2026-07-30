import 'dart:convert';

import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/lists/application/lists_state.dart';
import 'package:vocanova_mobile/features/lists/data/lists_repository.dart';
import 'package:vocanova_mobile/features/lists/domain/user_list.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

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
    final cachedPersonalTopics = _decodePersonalTopics(
      await storage.get<String>(StorageKeys.personalTopicsCacheJson),
    );
    final online = await ref.read(connectivityServiceProvider).isOnline;
    if (!online) {
      state = ListsState(
        lists: cached,
        personalTopics: cachedPersonalTopics,
        isLoading: false,
        isOffline: true,
      );
      return;
    }

    try {
      final repository = ref.read(listsRepositoryProvider);
      final listsFuture = repository.getLists();
      final personalTopicsFuture = repository.getPersonalTopics();
      final sources = await Future.wait<Object>([
        listsFuture,
        personalTopicsFuture,
      ]);
      final lists = sources[0] as List<UserList>;
      final personalTopics = sources[1] as List<PersonalTopicSummary>;
      state = ListsState(
        lists: lists,
        personalTopics: personalTopics,
        isLoading: false,
      );
      await Future.wait([
        _cacheLists(lists),
        _cachePersonalTopics(personalTopics),
      ]);
    } catch (_) {
      state = ListsState(
        lists: cached,
        personalTopics: cachedPersonalTopics,
        isLoading: false,
        isOffline: cached.isNotEmpty || cachedPersonalTopics.isNotEmpty,
        errorMessage: cached.isEmpty && cachedPersonalTopics.isEmpty
            ? _l10n.listsLoadListsError
            : null,
      );
    }
  }

  Future<bool> create(String name) async {
    if (!_canMutate()) return false;
    state = state.copyWith(isMutating: true, clearError: true);
    try {
      final created = await ref
          .read(listsRepositoryProvider)
          .create(name.trim());
      final lists = [created, ...state.lists];
      state = state.copyWith(lists: lists, isMutating: false);
      await _cacheLists(lists);
      return true;
    } catch (_) {
      state = state.copyWith(
        isMutating: false,
        errorMessage: _l10n.listsCreateError,
      );
      return false;
    }
  }

  Future<bool> rename({required int listId, required String name}) async {
    if (!_canMutate()) return false;
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
      await _cacheLists(lists);
      return true;
    } catch (_) {
      state = state.copyWith(
        isMutating: false,
        errorMessage: _l10n.listsRenameError,
      );
      return false;
    }
  }

  Future<bool> delete(int listId) async {
    if (!_canMutate()) return false;
    final index = state.lists.indexWhere((list) => list.listId == listId);
    if (index < 0) return false;
    final removed = state.lists[index];
    final optimistic = [...state.lists]..removeAt(index);
    state = state.copyWith(lists: optimistic, clearError: true);
    try {
      await ref.read(listsRepositoryProvider).delete(listId);
      await _cacheLists(optimistic);
      return true;
    } catch (_) {
      final rollback = [...state.lists]..insert(index, removed);
      state = state.copyWith(
        lists: rollback,
        errorMessage: _l10n.listsDeleteError,
      );
      return false;
    }
  }

  Future<void> _cacheLists(List<UserList> lists) => ref
      .read(listsLocalStorageProvider)
      .set(
        StorageKeys.listsCacheJson,
        jsonEncode(lists.map((list) => list.toJson()).toList()),
      );

  Future<void> _cachePersonalTopics(List<PersonalTopicSummary> topics) => ref
      .read(listsLocalStorageProvider)
      .set(
        StorageKeys.personalTopicsCacheJson,
        jsonEncode(topics.map((topic) => topic.toJson()).toList()),
      );

  bool _canMutate() {
    if (!state.isOffline) return true;
    state = state.copyWith(
      isOffline: true,
      errorMessage: _l10n.listsOfflineMutateError,
    );
    return false;
  }

  AppLocalizations get _l10n =>
      lookupAppLocalizations(AppSettingsNotifier.instance.state.locale);

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

  List<PersonalTopicSummary> _decodePersonalTopics(String? source) {
    try {
      return (jsonDecode(source ?? '[]') as List<dynamic>)
          .whereType<Map<String, dynamic>>()
          .map(PersonalTopicSummary.fromJson)
          .where((topic) => topic.listId != null)
          .toList(growable: false);
    } on Object {
      return const [];
    }
  }
}
