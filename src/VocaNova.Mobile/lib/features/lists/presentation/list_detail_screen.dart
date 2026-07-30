import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/topic_display_name.dart';
import 'package:vocanova_mobile/features/lists/application/list_detail_notifier.dart';
import 'package:vocanova_mobile/features/lists/application/list_detail_state.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/domain/list_word.dart';
import 'package:vocanova_mobile/features/lists/presentation/add_word_sheet.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class ListDetailScreen extends ConsumerStatefulWidget {
  const ListDetailScreen({required this.listId, super.key});

  final int listId;

  @override
  ConsumerState<ListDetailScreen> createState() => _ListDetailScreenState();
}

class _ListDetailScreenState extends ConsumerState<ListDetailScreen> {
  late final ScrollController _scrollController;

  @override
  void initState() {
    super.initState();
    _scrollController = ScrollController()..addListener(_onScroll);
    Future.microtask(
      () => ref.read(listDetailProvider(widget.listId).notifier).load(),
    );
  }

  @override
  void dispose() {
    _scrollController
      ..removeListener(_onScroll)
      ..dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(listDetailProvider(widget.listId));
    final l10n = AppLocalizations.of(context)!;
    ref.listen(
      listDetailProvider(widget.listId).select((value) => value.errorMessage),
      (previous, next) {
        if (next != null && next != previous) {
          ScaffoldMessenger.of(
            context,
          ).showSnackBar(SnackBar(content: Text(next)));
        }
      },
    );
    final listName = ref
        .watch(listsProvider)
        .lists
        .where((list) => list.listId == widget.listId)
        .map((list) => list.listName)
        .firstOrNull;
    return Scaffold(
      appBar: AppBar(
        title: Text(listName ?? l10n.listsDetailTitleFallback),
      ),
      floatingActionButton: FloatingActionButton.extended(
        key: const Key('add-word-fab'),
        onPressed: state.isOffline
            ? null
            : () => showModalBottomSheet<void>(
                context: context,
                isScrollControlled: true,
                builder: (_) => AddWordSheet(listId: widget.listId),
              ),
        icon: const Icon(Icons.add),
        label: Text(l10n.listsAddWordAction),
      ),
      body: Column(
        children: [
          if (state.isOffline) const _OfflineBanner(),
          _Actions(
            listId: widget.listId,
            isOffline: state.isOffline,
            onRandom: _showRandomDialog,
          ),
          Expanded(child: _content(state)),
        ],
      ),
    );
  }

  Widget _content(ListDetailState state) {
    final l10n = AppLocalizations.of(context)!;
    if (state.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (state.words.isEmpty) {
      return Center(child: Text(l10n.listsDetailEmpty));
    }
    return RefreshIndicator(
      onRefresh: ref.read(listDetailProvider(widget.listId).notifier).load,
      child: ListView.builder(
        key: const Key('list-words'),
        controller: _scrollController,
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 96),
        itemCount: state.words.length + (state.isLoadingMore ? 1 : 0),
        itemBuilder: (_, index) {
          if (index == state.words.length) {
            return const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            );
          }
          final word = state.words[index];
          return Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: Dismissible(
              key: Key('list-word-${word.wordId}'),
              direction: state.isOffline
                  ? DismissDirection.none
                  : DismissDirection.endToStart,
              confirmDismiss: (_) => _confirmRemove(word),
              onDismissed: (_) => ref
                  .read(listDetailProvider(widget.listId).notifier)
                  .removeWord(word.wordId),
              background: Container(
                alignment: Alignment.centerRight,
                padding: const EdgeInsets.only(right: 24),
                decoration: BoxDecoration(
                  color: AppColors.error,
                  borderRadius: BorderRadius.circular(20),
                ),
                child: const Icon(Icons.delete, color: Colors.white),
              ),
              child: _ListWordCard(
                word: word,
                alternate: index.isOdd,
                onTap: () =>
                    context.push(AppRoutes.wordDetail(word.wordId.toString())),
              ),
            ),
          );
        },
      ),
    );
  }

  void _onScroll() {
    if (_scrollController.position.extentAfter < 300) {
      ref.read(listDetailProvider(widget.listId).notifier).loadMore();
    }
  }

  Future<bool> _confirmRemove(ListWord word) async {
    final l10n = AppLocalizations.of(context)!;
    return await showDialog<bool>(
          context: context,
          builder: (context) => AlertDialog(
            title: Text(l10n.listsRemoveWordConfirmTitle),
            content: Text(l10n.listsRemoveWordConfirmBody(word.word)),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context, false),
                child: Text(l10n.listsCancel),
              ),
              FilledButton(
                key: const Key('confirm-remove-word'),
                onPressed: () => Navigator.pop(context, true),
                child: Text(l10n.listsDeleteAction),
              ),
            ],
          ),
        ) ??
        false;
  }

  Future<void> _showRandomDialog() async {
    final l10n = AppLocalizations.of(context)!;
    List<TopicSummary> topics;
    try {
      topics = await ref.read(wordSearchRepositoryProvider).getTopics();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(l10n.listsLoadTopicsError)),
        );
      }
      return;
    }
    if (!mounted) return;
    String method = 'random_topic';
    int count = 5;
    int? topicId = topics.firstOrNull?.topicId;
    final result = await showDialog<_RandomRequest>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text(l10n.listsAddRandomDialogTitle),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DropdownButtonFormField<String>(
                key: const Key('random-method'),
                initialValue: method,
                items: [
                  DropdownMenuItem(
                    value: 'random_topic',
                    child: Text(l10n.listsByTopicOption),
                  ),
                  DropdownMenuItem(
                    value: 'random_synonym',
                    child: Text(l10n.listsSynonymOption),
                  ),
                  DropdownMenuItem(
                    value: 'random_antonym',
                    child: Text(l10n.listsAntonymOption),
                  ),
                ],
                onChanged: (value) =>
                    setDialogState(() => method = value ?? method),
              ),
              const SizedBox(height: 12),
              if (method == 'random_topic')
                DropdownButtonFormField<int>(
                  key: const Key('random-topic'),
                  initialValue: topicId,
                  items: [
                    for (final topic in topics)
                      DropdownMenuItem(
                        value: topic.topicId,
                        child: Text(topic.localizedName(context)),
                      ),
                  ],
                  onChanged: (value) => setDialogState(() => topicId = value),
                ),
              const SizedBox(height: 12),
              TextFormField(
                key: const Key('random-count'),
                initialValue: count.toString(),
                keyboardType: TextInputType.number,
                decoration: InputDecoration(
                  labelText: l10n.listsCountFieldLabel,
                ),
                onChanged: (value) => count = int.tryParse(value) ?? 0,
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text(l10n.listsCancel),
            ),
            FilledButton(
              key: const Key('submit-random-words'),
              onPressed: () {
                if (count >= 1 &&
                    count <= 50 &&
                    (method != 'random_topic' || topicId != null)) {
                  Navigator.pop(
                    context,
                    _RandomRequest(
                      method: method,
                      count: count,
                      topicId: topicId,
                    ),
                  );
                }
              },
              child: Text(l10n.listsAddAction),
            ),
          ],
        ),
      ),
    );
    if (result != null) {
      await ref
          .read(listDetailProvider(widget.listId).notifier)
          .addRandom(
            count: result.count,
            method: result.method,
            topicId: result.topicId,
          );
    }
  }
}

class _Actions extends StatelessWidget {
  const _Actions({
    required this.listId,
    required this.isOffline,
    required this.onRandom,
  });

  final int listId;
  final bool isOffline;
  final VoidCallback onRandom;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
      child: Row(
        children: [
          Expanded(
            child: OutlinedButton.icon(
              key: const Key('add-random-words'),
              onPressed: isOffline ? null : onRandom,
              icon: const Icon(Icons.shuffle),
              label: Text(l10n.listsAddRandomAction),
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: FilledButton.icon(
              key: const Key('start-list-quiz'),
              onPressed: isOffline
                  ? null
                  : () => context.push(
                      AppRoutes.quizConfigForList(listId.toString()),
                    ),
              icon: const Icon(Icons.quiz_outlined),
              label: Text(l10n.listsStartQuizAction),
            ),
          ),
        ],
      ),
    );
  }
}

class _ListWordCard extends StatelessWidget {
  const _ListWordCard({
    required this.word,
    required this.onTap,
    this.alternate = false,
  });

  final ListWord word;
  final VoidCallback onTap;

  /// Dòng lẻ đổi nền nhạt hơn để hai dòng liền nhau dễ phân biệt khi lướt.
  final bool alternate;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final l10n = AppLocalizations.of(context)!;
    return Card(
      // Nền trắng/xám so le để hai dòng liền nhau tách bạch khi lướt. Dòng lẻ
      // đậm màu chữ 7% lên nền để luôn thấy rõ dù ở light hay dark theme.
      color: alternate
          ? Color.alphaBlend(
              scheme.onSurface.withValues(alpha: 0.07),
              scheme.surface,
            )
          : scheme.surface,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(20),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            // Kéo giãn hết chiều ngang để nền phủ nguyên dòng, không co lại theo
            // độ dài nội dung.
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                word.word,
                style: Theme.of(
                  context,
                ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
              ),
              if (word.primaryMeaning != null) Text(word.primaryMeaning!),
              const SizedBox(height: 8),
              Wrap(
                spacing: 12,
                children: [
                  Text(l10n.listsCorrectCount(word.correctCount)),
                  Text(l10n.listsWrongCount(word.wrongCount)),
                ],
              ),
              if (word.note?.trim().isNotEmpty == true) ...[
                const SizedBox(height: 8),
                Text(l10n.listsNoteLabel(word.note!)),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _OfflineBanner extends StatelessWidget {
  const _OfflineBanner();

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Container(
      key: const Key('list-detail-offline-banner'),
      width: double.infinity,
      color: AppColors.error.withValues(alpha: 0.18),
      padding: const EdgeInsets.all(8),
      child: Text(l10n.listsDetailOfflineBanner, textAlign: TextAlign.center),
    );
  }
}

class _RandomRequest {
  const _RandomRequest({
    required this.method,
    required this.count,
    this.topicId,
  });

  final String method;
  final int count;
  final int? topicId;
}
