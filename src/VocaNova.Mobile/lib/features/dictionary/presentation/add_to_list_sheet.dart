import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_detail_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_detail.dart';

class AddToListSheet extends ConsumerStatefulWidget {
  const AddToListSheet({required this.wordId, super.key});

  final int wordId;

  @override
  ConsumerState<AddToListSheet> createState() => _AddToListSheetState();
}

class _AddToListSheetState extends ConsumerState<AddToListSheet> {
  late final Future<List<UserListSummary>> _lists;
  int? _addingListId;

  @override
  void initState() {
    super.initState();
    _lists = ref.read(wordDetailRepositoryProvider).getLists();
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 12, 20, 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Thêm vào danh sách',
              style: Theme.of(
                context,
              ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 12),
            FutureBuilder<List<UserListSummary>>(
              future: _lists,
              builder: (context, snapshot) {
                if (snapshot.connectionState != ConnectionState.done) {
                  return const Center(child: CircularProgressIndicator());
                }
                if (snapshot.hasError) {
                  return const Padding(
                    padding: EdgeInsets.symmetric(vertical: 24),
                    child: Text('Không thể tải danh sách từ.'),
                  );
                }
                final lists = snapshot.data ?? const [];
                if (lists.isEmpty) {
                  return const Padding(
                    padding: EdgeInsets.symmetric(vertical: 24),
                    child: Text('Bạn chưa có danh sách từ nào.'),
                  );
                }
                return Flexible(
                  child: ListView(
                    shrinkWrap: true,
                    children: [
                      for (final list in lists)
                        ListTile(
                          key: Key('add-to-list-${list.listId}'),
                          leading: const Icon(Icons.bookmark_outline),
                          title: Text(list.listName),
                          subtitle: Text('${list.wordCount} từ'),
                          trailing: _addingListId == list.listId
                              ? const SizedBox.square(
                                  dimension: 20,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                  ),
                                )
                              : const Icon(Icons.add),
                          onTap: _addingListId == null
                              ? () => _add(list)
                              : null,
                        ),
                    ],
                  ),
                );
              },
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _add(UserListSummary list) async {
    setState(() => _addingListId = list.listId);
    try {
      await ref
          .read(wordDetailRepositoryProvider)
          .addWordToList(listId: list.listId, wordId: widget.wordId);
      if (!mounted) return;
      Navigator.pop(context);
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Đã thêm vào ${list.listName}.')));
    } catch (_) {
      if (!mounted) return;
      setState(() => _addingListId = null);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Không thể thêm từ vào danh sách.')),
      );
    }
  }
}
