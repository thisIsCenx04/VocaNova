import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:shimmer/shimmer.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_state.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/word_summary_card.dart';

class WordSearchScreen extends ConsumerStatefulWidget {
  const WordSearchScreen({super.key});

  @override
  ConsumerState<WordSearchScreen> createState() => _WordSearchScreenState();
}

class _WordSearchScreenState extends ConsumerState<WordSearchScreen> {
  static const cefrLevels = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'];

  final _controller = TextEditingController();

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(wordSearchProvider.notifier).initialize());
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(wordSearchProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Tra từ')),
      body: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 10),
              child: TextField(
                key: const Key('word-search-field'),
                controller: _controller,
                textInputAction: TextInputAction.search,
                decoration: InputDecoration(
                  hintText: 'Nhập từ tiếng Anh cần tìm',
                  prefixIcon: const Icon(Icons.search),
                  suffixIcon: state.query.isEmpty
                      ? null
                      : IconButton(
                          tooltip: 'Xóa tìm kiếm',
                          onPressed: () {
                            _controller.clear();
                            ref
                                .read(wordSearchProvider.notifier)
                                .queryChanged('');
                          },
                          icon: const Icon(Icons.close),
                        ),
                ),
                onChanged: ref.read(wordSearchProvider.notifier).queryChanged,
                onSubmitted: (_) =>
                    ref.read(wordSearchProvider.notifier).search(),
              ),
            ),
            if (state.isOffline) const _OfflineBanner(),
            _Filters(state: state),
            Expanded(child: _content(state)),
          ],
        ),
      ),
    );
  }

  Widget _content(WordSearchState state) {
    if (state.query.trim().isEmpty) {
      return _History(
        history: state.history,
        onClear: ref.read(wordSearchProvider.notifier).clearHistory,
        onSelect: (word) {
          _controller.text = word;
          _controller.selection = TextSelection.collapsed(offset: word.length);
          ref.read(wordSearchProvider.notifier).useHistory(word);
        },
      );
    }
    if (state.isLoading) {
      return const _LoadingSkeleton();
    }
    if (state.results.isEmpty) {
      return const Center(child: Text('Không tìm thấy từ phù hợp.'));
    }
    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
      itemCount: state.results.length,
      separatorBuilder: (_, _) => const SizedBox(height: 10),
      itemBuilder: (context, index) {
        final word = state.results[index];
        return WordSummaryCard(
          word: word,
          onTap: () {
            ref.read(wordSearchProvider.notifier).recordHistory(word.word);
            context.push(AppRoutes.wordDetail(word.wordId.toString()));
          },
        );
      },
    );
  }
}

class _Filters extends ConsumerWidget {
  const _Filters({required this.state});

  final WordSearchState state;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          height: 46,
          child: ListView(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            scrollDirection: Axis.horizontal,
            children: [
              FilterChip(
                key: const Key('cefr-all'),
                label: const Text('Tất cả'),
                selected: state.selectedCefr == null,
                onSelected: (_) =>
                    ref.read(wordSearchProvider.notifier).selectCefr(null),
              ),
              for (final level in _WordSearchScreenState.cefrLevels) ...[
                const SizedBox(width: 8),
                FilterChip(
                  key: Key('cefr-$level'),
                  label: Text(level),
                  selected: state.selectedCefr == level,
                  onSelected: (_) =>
                      ref.read(wordSearchProvider.notifier).selectCefr(level),
                ),
              ],
            ],
          ),
        ),
        if (state.topics.isNotEmpty)
          SizedBox(
            height: 46,
            child: ListView(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              scrollDirection: Axis.horizontal,
              children: [
                FilterChip(
                  key: const Key('topic-all'),
                  label: const Text('Mọi chủ đề'),
                  selected: state.selectedTopicId == null,
                  onSelected: (_) =>
                      ref.read(wordSearchProvider.notifier).selectTopic(null),
                ),
                for (final topic in state.topics) ...[
                  const SizedBox(width: 8),
                  FilterChip(
                    key: Key('topic-${topic.topicId}'),
                    label: Text(topic.displayName),
                    selected: state.selectedTopicId == topic.topicId,
                    onSelected: (_) => ref
                        .read(wordSearchProvider.notifier)
                        .selectTopic(topic.topicId),
                  ),
                ],
              ],
            ),
          ),
      ],
    );
  }
}

class _OfflineBanner extends StatelessWidget {
  const _OfflineBanner();

  @override
  Widget build(BuildContext context) {
    return Container(
      key: const Key('offline-banner'),
      width: double.infinity,
      color: AppColors.error.withValues(alpha: 0.18),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: const Text(
        'Bạn đang offline. Kết quả chỉ lấy từ dữ liệu đã lưu.',
        textAlign: TextAlign.center,
      ),
    );
  }
}

class _History extends StatelessWidget {
  const _History({
    required this.history,
    required this.onClear,
    required this.onSelect,
  });

  final List<String> history;
  final VoidCallback onClear;
  final ValueChanged<String> onSelect;

  @override
  Widget build(BuildContext context) {
    if (history.isEmpty) {
      return const Center(child: Text('Chưa có lịch sử tìm kiếm.'));
    }
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
      children: [
        Row(
          children: [
            Text(
              'Tìm kiếm gần đây',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const Spacer(),
            TextButton(
              key: const Key('clear-search-history'),
              onPressed: onClear,
              child: const Text('Xóa'),
            ),
          ],
        ),
        for (final word in history)
          ListTile(
            leading: const Icon(Icons.history),
            title: Text(word),
            trailing: const Icon(Icons.north_west),
            onTap: () => onSelect(word),
          ),
      ],
    );
  }
}

class _LoadingSkeleton extends StatelessWidget {
  const _LoadingSkeleton();

  @override
  Widget build(BuildContext context) {
    return Shimmer.fromColors(
      baseColor: Theme.of(context).colorScheme.surface,
      highlightColor: Theme.of(
        context,
      ).colorScheme.onSurface.withValues(alpha: 0.12),
      child: ListView.separated(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
        itemCount: 5,
        separatorBuilder: (_, _) => const SizedBox(height: 10),
        itemBuilder: (_, index) => Container(
          key: index == 0 ? const Key('search-loading-skeleton') : null,
          height: 112,
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(20),
          ),
        ),
      ),
    );
  }
}
