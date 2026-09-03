import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/models/word_summary.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/add_to_list_sheet.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/topic_display_name.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/topic_icon.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class TopicDetailScreen extends ConsumerStatefulWidget {
  const TopicDetailScreen({
    required this.topicId,
    this.isPersonal = false,
    this.initialTopic,
    super.key,
  });
  final int topicId;
  final bool isPersonal;
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
    final apiService = ref.read(wordSearchApiServiceProvider);
    _words = widget.isPersonal
        ? apiService.getPersonalTopicWords(widget.topicId)
        : apiService.getTopicWords(widget.topicId);
  }

  @override
  Widget build(BuildContext context) {
    final topic = widget.initialTopic;
    final l10n = AppLocalizations.of(context)!;
    return Scaffold(
      appBar: AppBar(
        title: Text(
          widget.isPersonal
              ? l10n.dictMyTopicTitle(
                  topic?.localizedName(context) ?? l10n.dictTopicFallbackName,
                )
              : topic?.localizedName(context) ??
                    l10n.dictTopicDetailFallbackTitle,
        ),
      ),
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
                child: Text(
                  AppLocalizations.of(context)!.dictUnableToLoadWordsRetry,
                ),
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
                if (widget.isPersonal &&
                    topic is PersonalTopicSummary &&
                    topic.listId != null) ...[
                  const SizedBox(height: 12),
                  SizedBox(
                    width: double.infinity,
                    child: FilledButton.icon(
                      key: const Key('practice-personal-topic'),
                      onPressed: () => context.push(
                        AppRoutes.quizConfigForList('${topic.listId}'),
                      ),
                      icon: const Icon(Icons.quiz_outlined),
                      label: Text(
                        AppLocalizations.of(context)!.dictPracticeSavedWords,
                      ),
                    ),
                  ),
                ],
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
                          label: Text(_tabLabel(context, tab)),
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
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 48),
                    child: Center(
                      child: Text(
                        AppLocalizations.of(context)!.dictNoWordsInCategory,
                      ),
                    ),
                  )
                else
                  for (final (index, word) in visible.indexed)
                    _TopicWordTile(
                      word: word,
                      alternate: index.isOdd,
                      onOpen: () =>
                          context.push(AppRoutes.wordDetail('${word.wordId}')),
                      onAdd: () => showModalBottomSheet<void>(
                        context: context,
                        isScrollControlled: true,
                        useSafeArea: true,
                        builder: (_) => AddToListSheet(wordId: word.wordId),
                      ),
                      onRemove: widget.isPersonal
                          ? () => _removePersonalWord(word.wordId)
                          : null,
                    ),
              ],
            ),
          );
        },
      ),
    );
  }

  Future<void> _removePersonalWord(int wordId) async {
    try {
      await ref
          .read(wordSearchApiServiceProvider)
          .removePersonalTopicWord(topicId: widget.topicId, wordId: wordId);
      if (!mounted) return;
      setState(_load);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(AppLocalizations.of(context)!.dictRemovedFromTopic),
        ),
      );
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(AppLocalizations.of(context)!.dictUnableToRemoveWord),
        ),
      );
    }
  }

  /// [tab] is one of the internal English identifiers in [_tabs] (also used
  /// for widget keys and matched against [WordSummary.learningStatus]); this
  /// only maps it to the localized label shown on the chip.
  String _tabLabel(BuildContext context, String tab) {
    final l10n = AppLocalizations.of(context)!;
    switch (tab) {
      case 'New':
        return l10n.dictStatNew;
      case 'Learning':
        return l10n.dictStatLearning;
      case 'Mastered':
        return l10n.dictStatMastered;
      default:
        return l10n.dictCategoryAll;
    }
  }
}

class _TopicHero extends StatelessWidget {
  const _TopicHero({required this.topic});
  final TopicSummary topic;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      TopicIcon(icon: topic.icon, name: topic.name, size: 42),
      const SizedBox(width: 14),
      Expanded(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              topic.localizedName(context),
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            Text(
              AppLocalizations.of(context)!.dictWordCountLabel(topic.wordCount),
            ),
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
    final l10n = AppLocalizations.of(context)!;
    final values = [
      (l10n.dictStatMastered, '${count('mastered')}'),
      (l10n.dictStatLearning, '${count('learning')}'),
      (l10n.dictStatNew, '${count('new')}'),
      (l10n.dictStatAvgMastery, '${average.toStringAsFixed(0)}%'),
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
    this.onRemove,
    this.alternate = false,
  });
  final WordSummary word;
  final VoidCallback onOpen;
  final VoidCallback onAdd;
  final VoidCallback? onRemove;

  /// Dòng lẻ đổi nền nhạt hơn để hai dòng liền nhau dễ phân biệt khi lướt.
  final bool alternate;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      color: alternate
          ? Color.alphaBlend(
              scheme.onSurface.withValues(alpha: 0.07),
              scheme.surface,
            )
          : scheme.surface,
      child: ListTile(
        key: Key('topic-word-${word.wordId}'),
        onTap: onOpen,
        title: Text(word.word),
        subtitle: Text(word.primaryMeaning ?? word.phonetic ?? ''),
        trailing: IconButton(
          key: Key(
            onRemove == null
                ? 'add-topic-word-${word.wordId}'
                : 'remove-personal-topic-word-${word.wordId}',
          ),
          tooltip: onRemove == null
              ? AppLocalizations.of(context)!.dictAddToListTooltip
              : AppLocalizations.of(context)!.dictRemoveFromTopicTooltip,
          onPressed: onRemove ?? onAdd,
          icon: Icon(
            onRemove == null ? Icons.playlist_add : Icons.remove_circle_outline,
          ),
        ),
      ),
    );
  }
}
