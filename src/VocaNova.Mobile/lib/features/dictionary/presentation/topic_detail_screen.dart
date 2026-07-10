import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/add_to_list_sheet.dart';

class TopicDetailScreen extends ConsumerStatefulWidget {
  const TopicDetailScreen({
    required this.topicId,
    this.initialTopic,
    super.key,
  });
  final int topicId;
  final TopicSummary? initialTopic;

  @override
  ConsumerState<TopicDetailScreen> createState() => _TopicDetailScreenState();
}

class _TopicDetailScreenState extends ConsumerState<TopicDetailScreen> {
  static const _tabs = ['All', 'New', 'Learning', 'Mastered'];
  late Future<List<WordSummary>> _words;
  String _filter = 'All';

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() {
    _words = ref
        .read(wordSearchRepositoryProvider)
        .getTopicWords(widget.topicId);
  }

  @override
  Widget build(BuildContext context) {
    final topic = widget.initialTopic;
    return Scaffold(
      appBar: AppBar(title: Text(topic?.displayName ?? 'Topic detail')),
      body: FutureBuilder<List<WordSummary>>(
        future: _words,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return Center(
              child: TextButton(
                onPressed: () => setState(_load),
                child: const Text('Unable to load words. Try again'),
              ),
            );
          }
          final all = snapshot.data ?? const <WordSummary>[];
          final visible = _filter == 'All'
              ? all
              : all
                    .where(
                      (word) => word.learningStatus == _filter.toLowerCase(),
                    )
                    .toList();
          return RefreshIndicator(
            onRefresh: () async {
              setState(_load);
              await _words;
            },
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
              children: [
                if (topic != null) _TopicHero(topic: topic),
                const SizedBox(height: 14),
                _Stats(words: all),
                const SizedBox(height: 16),
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(
                    children: [
                      for (final tab in _tabs) ...[
                        ChoiceChip(
                          key: Key('topic-filter-${tab.toLowerCase()}'),
                          label: Text(tab),
                          selected: _filter == tab,
                          onSelected: (_) => setState(() => _filter = tab),
                        ),
                        const SizedBox(width: 8),
                      ],
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                if (visible.isEmpty)
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 48),
                    child: Center(child: Text('No words in this category.')),
                  )
                else
                  for (final word in visible)
                    _TopicWordTile(
                      word: word,
                      onOpen: () =>
                          context.push(AppRoutes.wordDetail('${word.wordId}')),
                      onAdd: () => showModalBottomSheet<void>(
                        context: context,
                        isScrollControlled: true,
                        useSafeArea: true,
                        builder: (_) => AddToListSheet(wordId: word.wordId),
                      ),
                    ),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _TopicHero extends StatelessWidget {
  const _TopicHero({required this.topic});
  final TopicSummary topic;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Text(
        topic.icon?.trim().isNotEmpty == true ? topic.icon! : '📚',
        style: const TextStyle(fontSize: 42),
      ),
      const SizedBox(width: 14),
      Expanded(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              topic.displayName,
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            Text('${topic.wordCount} words'),
          ],
        ),
      ),
    ],
  );
}

class _Stats extends StatelessWidget {
  const _Stats({required this.words});
  final List<WordSummary> words;

  @override
  Widget build(BuildContext context) {
    int count(String status) =>
        words.where((word) => word.learningStatus == status).length;
    final average = words.isEmpty
        ? 0.0
        : words.fold<double>(0, (sum, word) => sum + word.masteryScore) /
              words.length;
    final values = [
      ('Mastered', '${count('mastered')}'),
      ('Learning', '${count('learning')}'),
      ('New', '${count('new')}'),
      ('Avg mastery', '${average.toStringAsFixed(0)}%'),
    ];
    return Row(
      key: const Key('topic-stats'),
      children: [
        for (final value in values)
          Expanded(
            child: Column(
              children: [
                Text(
                  value.$2,
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    color: AppColors.primary,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                Text(
                  value.$1,
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.labelSmall,
                ),
              ],
            ),
          ),
      ],
    );
  }
}

class _TopicWordTile extends StatelessWidget {
  const _TopicWordTile({
    required this.word,
    required this.onOpen,
    required this.onAdd,
  });
  final WordSummary word;
  final VoidCallback onOpen;
  final VoidCallback onAdd;

  @override
  Widget build(BuildContext context) => Card(
    margin: const EdgeInsets.only(bottom: 10),
    child: ListTile(
      key: Key('topic-word-${word.wordId}'),
      onTap: onOpen,
      title: Text(word.word),
      subtitle: Text(word.primaryMeaning ?? word.phonetic ?? ''),
      trailing: IconButton(
        key: Key('add-topic-word-${word.wordId}'),
        tooltip: 'Add to list',
        onPressed: onAdd,
        icon: const Icon(Icons.playlist_add),
      ),
    ),
  );
}
