import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/lists/application/list_detail_notifier.dart';
import 'package:vocanova_mobile/features/lists/application/list_detail_state.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/domain/list_word.dart';
import 'package:vocanova_mobile/features/lists/presentation/add_word_sheet.dart';

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
      appBar: AppBar(title: Text(listName ?? 'Chi tiết danh sách')),
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
        label: const Text('Thêm từ'),
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
    if (state.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (state.words.isEmpty) {
      return const Center(child: Text('Danh sách chưa có từ nào.'));
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
    return await showDialog<bool>(
          context: context,
          builder: (context) => AlertDialog(
            title: const Text('Xóa từ?'),
            content: Text('Xóa "${word.word}" khỏi danh sách?'),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context, false),
                child: const Text('Hủy'),
              ),
              FilledButton(
                key: const Key('confirm-remove-word'),
                onPressed: () => Navigator.pop(context, true),
                child: const Text('Xóa'),
              ),
            ],
          ),
        ) ??
        false;
  }

  Future<void> _showRandomDialog() async {
    List<TopicSummary> topics;
    try {
      topics = await ref.read(wordSearchRepositoryProvider).getTopics();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Không thể tải chủ đề.')));
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
          title: const Text('Thêm từ ngẫu nhiên'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DropdownButtonFormField<String>(
                key: const Key('random-method'),
                initialValue: method,
                items: const [
                  DropdownMenuItem(
                    value: 'random_topic',
                    child: Text('Theo chủ đề'),
                  ),
                  DropdownMenuItem(
                    value: 'random_synonym',
                    child: Text('Từ đồng nghĩa'),
                  ),
                  DropdownMenuItem(
                    value: 'random_antonym',
                    child: Text('Từ trái nghĩa'),
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
                        child: Text(topic.displayName),
                      ),
                  ],
                  onChanged: (value) => setDialogState(() => topicId = value),
                ),
              const SizedBox(height: 12),
              TextFormField(
                key: const Key('random-count'),
                initialValue: count.toString(),
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(labelText: 'Số lượng (1-50)'),
                onChanged: (value) => count = int.tryParse(value) ?? 0,
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Hủy'),
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
              child: const Text('Thêm'),
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
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
      child: Row(
        children: [
          Expanded(
            child: OutlinedButton.icon(
              key: const Key('add-random-words'),
              onPressed: isOffline ? null : onRandom,
              icon: const Icon(Icons.shuffle),
              label: const Text('Thêm ngẫu nhiên'),
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
              label: const Text('Bắt đầu kiểm tra'),
            ),
          ),
        ],
      ),
    );
  }
}

class _ListWordCard extends StatelessWidget {
  const _ListWordCard({required this.word, required this.onTap});

  final ListWord word;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(20),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
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
                  Text('Đúng: ${word.correctCount}'),
                  Text('Sai: ${word.wrongCount}'),
                ],
              ),
              if (word.note?.trim().isNotEmpty == true) ...[
                const SizedBox(height: 8),
                Text('Ghi chú: ${word.note}'),
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
    return Container(
      key: const Key('list-detail-offline-banner'),
      width: double.infinity,
      color: AppColors.error.withValues(alpha: 0.18),
      padding: const EdgeInsets.all(8),
      child: const Text(
        'Bạn đang offline. Đang hiển thị các từ đã lưu.',
        textAlign: TextAlign.center,
      ),
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
