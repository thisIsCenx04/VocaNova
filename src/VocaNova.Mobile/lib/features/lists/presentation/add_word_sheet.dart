import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/lists/application/list_detail_notifier.dart';

class AddWordSheet extends ConsumerStatefulWidget {
  const AddWordSheet({required this.listId, super.key});

  final int listId;

  @override
  ConsumerState<AddWordSheet> createState() => _AddWordSheetState();
}

class _AddWordSheetState extends ConsumerState<AddWordSheet> {
  Timer? _debounce;
  List<WordSummary> _results = const [];
  bool _loading = false;
  int? _addingWordId;

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: EdgeInsets.fromLTRB(
          16,
          16,
          16,
          MediaQuery.viewInsetsOf(context).bottom + 16,
        ),
        child: SizedBox(
          height: MediaQuery.sizeOf(context).height * 0.65,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Thêm từ',
                style: Theme.of(
                  context,
                ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: 12),
              TextField(
                key: const Key('add-word-search-field'),
                autofocus: true,
                decoration: const InputDecoration(
                  hintText: 'Tìm từ tiếng Anh',
                  prefixIcon: Icon(Icons.search),
                ),
                onChanged: _search,
              ),
              const SizedBox(height: 12),
              Expanded(
                child: _loading
                    ? const Center(child: CircularProgressIndicator())
                    : ListView.builder(
                        itemCount: _results.length,
                        itemBuilder: (_, index) {
                          final word = _results[index];
                          return ListTile(
                            key: Key('add-search-word-${word.wordId}'),
                            title: Text(word.word),
                            subtitle: Text(word.primaryMeaning ?? ''),
                            trailing: _addingWordId == word.wordId
                                ? const SizedBox.square(
                                    dimension: 20,
                                    child: CircularProgressIndicator(
                                      strokeWidth: 2,
                                    ),
                                  )
                                : const Icon(Icons.add),
                            onTap: _addingWordId == null
                                ? () => _add(word)
                                : null,
                          );
                        },
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _search(String query) {
    _debounce?.cancel();
    if (query.trim().isEmpty) {
      setState(() => _results = const []);
      return;
    }
    _debounce = Timer(const Duration(milliseconds: 300), () async {
      setState(() => _loading = true);
      try {
        final results = await ref
            .read(wordSearchRepositoryProvider)
            .search(query: query.trim());
        if (mounted) setState(() => _results = results);
      } catch (_) {
        if (mounted) setState(() => _results = const []);
      } finally {
        if (mounted) setState(() => _loading = false);
      }
    });
  }

  Future<void> _add(WordSummary word) async {
    setState(() => _addingWordId = word.wordId);
    final success = await ref
        .read(listDetailProvider(widget.listId).notifier)
        .addWord(word.wordId);
    if (!mounted) return;
    if (success) {
      Navigator.pop(context);
    } else {
      setState(() => _addingWordId = null);
    }
  }
}
