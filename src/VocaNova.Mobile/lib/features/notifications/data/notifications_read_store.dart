import 'dart:convert';

import 'package:vocanova_mobile/core/storage/local_storage.dart';

/// Tracks which notifications the user has read — per device.
///
/// Notifications are derived by the server (nothing is stored in the database),
/// so read/unread state lives here instead of on the backend. A notification's id
/// is the deleted word's id, so the read set is a set of ints persisted as a JSON
/// array (matching how the rest of the app stores lists via [LocalStorage]).
class NotificationsReadStore {
  const NotificationsReadStore({LocalStorage? storage}) : _storage = storage;

  final LocalStorage? _storage;

  static const _key = 'notifications_read_ids';

  LocalStorage get _local => _storage ?? LocalStorage.instance;

  Future<Set<int>> readIds() async {
    final source = await _local.get<String>(_key);
    if (source == null) {
      return <int>{};
    }
    try {
      return (jsonDecode(source) as List<dynamic>)
          .whereType<num>()
          .map((id) => id.toInt())
          .toSet();
    } on Object {
      return <int>{};
    }
  }

  Future<void> markRead(int id) => markAllRead([id]);

  Future<void> markAllRead(Iterable<int> ids) async {
    final current = await readIds()..addAll(ids);
    await _local.set<String>(_key, jsonEncode(current.toList(growable: false)));
  }
}
