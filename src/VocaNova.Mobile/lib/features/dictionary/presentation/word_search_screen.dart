import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:shimmer/shimmer.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_state.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/word_summary_card.dart';

class WordSearchScreen extends ConsumerStatefulWidget {
  const WordSearchScreen({super.key});

  @override
  ConsumerState<WordSearchScreen> createState() => _WordSearchScreenState();
}

class _WordSearchScreenState extends ConsumerState<WordSearchScreen> {
  static const cefrLevels = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'];

  final _controller = TextEditingController();
  final _focusNode = FocusNode();

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(wordSearchProvider.notifier).initialize());
  }

  @override
  void dispose() {
    _controller.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(wordSearchProvider);
    final palette = _SearchPalette.of(context);
    final showFilters =
        state.query.trim().isNotEmpty || state.selectedTopicId != null;

    return Scaffold(
      backgroundColor: palette.background,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            _SearchHeader(
              controller: _controller,
              focusNode: _focusNode,
              hasQuery: state.query.isNotEmpty,
              onBack: _goBack,
              onChanged: ref.read(wordSearchProvider.notifier).queryChanged,
              onClear: _clearSearch,
              onSubmitted: (_) =>
                  ref.read(wordSearchProvider.notifier).search(),
            ),
            if (state.isOffline) const _OfflineBanner(),
            if (showFilters) _Filters(state: state),
            Expanded(child: _content(state)),
          ],
        ),
      ),
    );
  }

  Widget _content(WordSearchState state) {
    final isDiscovery =
        state.query.trim().isEmpty && state.selectedTopicId == null;
    if (isDiscovery) {
      return _DiscoveryView(
        history: state.history,
        topics: state.topics,
        onClearHistory: ref.read(wordSearchProvider.notifier).clearHistory,
        onHistorySelected: _useHistory,
        onTopicSelected: (topicId) =>
            ref.read(wordSearchProvider.notifier).selectTopic(topicId),
      );
    }
    if (state.isLoading) {
      return const _LoadingSkeleton();
    }
    if (state.results.isEmpty) {
      return _EmptyResults(isOffline: state.isOffline);
    }
    return ListView.separated(
      key: const Key('word-search-results'),
      padding: const EdgeInsets.fromLTRB(20, 12, 20, 24),
      itemCount: state.results.length,
      separatorBuilder: (_, _) => const SizedBox(height: 8),
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

  void _goBack() {
    if (context.canPop()) {
      context.pop();
      return;
    }
    context.go(AppRoutes.home);
  }

  void _clearSearch() {
    _controller.clear();
    ref.read(wordSearchProvider.notifier).queryChanged('');
    _focusNode.requestFocus();
  }

  void _useHistory(String word) {
    _controller.text = word;
    _controller.selection = TextSelection.collapsed(offset: word.length);
    ref.read(wordSearchProvider.notifier).useHistory(word);
  }
}

class _SearchHeader extends StatelessWidget {
  const _SearchHeader({
    required this.controller,
    required this.focusNode,
    required this.hasQuery,
    required this.onBack,
    required this.onChanged,
    required this.onClear,
    required this.onSubmitted,
  });

  final TextEditingController controller;
  final FocusNode focusNode;
  final bool hasQuery;
  final VoidCallback onBack;
  final ValueChanged<String> onChanged;
  final VoidCallback onClear;
  final ValueChanged<String> onSubmitted;

  @override
  Widget build(BuildContext context) {
    final palette = _SearchPalette.of(context);
    return Container(
      height: 65,
      padding: const EdgeInsets.fromLTRB(12, 8, 20, 17),
      decoration: BoxDecoration(
        color: palette.surface,
        border: Border(bottom: BorderSide(color: palette.border, width: 1)),
      ),
      child: Row(
        children: [
          IconButton(
            key: const Key('search-back-button'),
            tooltip: 'Back',
            onPressed: onBack,
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints.tightFor(width: 28, height: 40),
            icon: Icon(Icons.arrow_back, size: 20, color: palette.text),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: SizedBox(
              height: 40,
              child: TextField(
                key: const Key('word-search-field'),
                controller: controller,
                focusNode: focusNode,
                textInputAction: TextInputAction.search,
                style: AppTextStyles.body.copyWith(
                  color: palette.text,
                  fontSize: 15,
                  height: 22.5 / 15,
                ),
                decoration: InputDecoration(
                  hintText: 'Search for a word...',
                  isDense: true,
                  filled: true,
                  fillColor: palette.input,
                  contentPadding: const EdgeInsets.symmetric(vertical: 8),
                  prefixIcon: Icon(
                    Icons.search,
                    size: 15,
                    color: palette.secondaryText,
                  ),
                  prefixIconConstraints: const BoxConstraints(
                    minWidth: 38,
                    minHeight: 40,
                  ),
                  suffixIcon: hasQuery
                      ? IconButton(
                          key: const Key('clear-word-search'),
                          tooltip: 'Clear search',
                          onPressed: onClear,
                          icon: Icon(
                            Icons.close,
                            size: 16,
                            color: palette.secondaryText,
                          ),
                        )
                      : null,
                  suffixIconConstraints: const BoxConstraints(
                    minWidth: 40,
                    minHeight: 40,
                  ),
                  border: _border(palette.border),
                  enabledBorder: _border(palette.border),
                  focusedBorder: _border(AppColors.primary, width: 1.5),
                ),
                onChanged: onChanged,
                onSubmitted: onSubmitted,
              ),
            ),
          ),
        ],
      ),
    );
  }

  OutlineInputBorder _border(Color color, {double width = 1.25}) {
    return OutlineInputBorder(
      borderRadius: BorderRadius.circular(14),
      borderSide: BorderSide(color: color, width: width),
    );
  }
}

class _Filters extends ConsumerWidget {
  const _Filters({required this.state});

  final WordSearchState state;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final palette = _SearchPalette.of(context);
    return ColoredBox(
      color: palette.surface,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _FilterRow(
            children: [
              _filterChip(
                context,
                key: const Key('cefr-all'),
                label: 'All levels',
                selected: state.selectedCefr == null,
                onSelected: () =>
                    ref.read(wordSearchProvider.notifier).selectCefr(null),
              ),
              for (final level in _WordSearchScreenState.cefrLevels)
                _filterChip(
                  context,
                  key: Key('cefr-$level'),
                  label: level,
                  selected: state.selectedCefr == level,
                  onSelected: () =>
                      ref.read(wordSearchProvider.notifier).selectCefr(level),
                ),
            ],
          ),
          if (state.topics.isNotEmpty)
            _FilterRow(
              children: [
                _filterChip(
                  context,
                  key: const Key('topic-all'),
                  label: 'All topics',
                  selected: state.selectedTopicId == null,
                  onSelected: () =>
                      ref.read(wordSearchProvider.notifier).selectTopic(null),
                ),
                for (final topic in state.topics)
                  _filterChip(
                    context,
                    key: Key('topic-${topic.topicId}'),
                    label: topic.name,
                    selected: state.selectedTopicId == topic.topicId,
                    onSelected: () => ref
                        .read(wordSearchProvider.notifier)
                        .selectTopic(topic.topicId),
                  ),
              ],
            ),
        ],
      ),
    );
  }

  Widget _filterChip(
    BuildContext context, {
    required Key key,
    required String label,
    required bool selected,
    required VoidCallback onSelected,
  }) {
    final palette = _SearchPalette.of(context);
    return FilterChip(
      key: key,
      label: Text(label),
      selected: selected,
      onSelected: (_) => onSelected(),
      showCheckmark: false,
      visualDensity: VisualDensity.compact,
      side: BorderSide(color: selected ? AppColors.primary : palette.border),
      backgroundColor: palette.input,
      selectedColor: AppColors.primary.withValues(alpha: 0.14),
      labelStyle: AppTextStyles.caption.copyWith(
        color: selected ? AppColors.primary : palette.text,
        fontSize: 12,
        height: 18 / 12,
        fontWeight: FontWeight.w600,
      ),
    );
  }
}

class _FilterRow extends StatelessWidget {
  const _FilterRow({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 42,
      child: ListView.separated(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 3),
        scrollDirection: Axis.horizontal,
        itemCount: children.length,
        separatorBuilder: (_, _) => const SizedBox(width: 8),
        itemBuilder: (_, index) => children[index],
      ),
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
      color: const Color(0xFFFFF1C2),
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 7),
      child: Text(
        'Offline — searching cached words and recent history only.',
        textAlign: TextAlign.center,
        style: AppTextStyles.caption.copyWith(
          color: const Color(0xFF5E4700),
          fontSize: 12,
          height: 18 / 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

class _DiscoveryView extends StatelessWidget {
  const _DiscoveryView({
    required this.history,
    required this.topics,
    required this.onClearHistory,
    required this.onHistorySelected,
    required this.onTopicSelected,
  });

  final List<String> history;
  final List<TopicSummary> topics;
  final VoidCallback onClearHistory;
  final ValueChanged<String> onHistorySelected;
  final ValueChanged<int> onTopicSelected;

  @override
  Widget build(BuildContext context) {
    final palette = _SearchPalette.of(context);
    return ListView(
      key: const Key('search-discovery'),
      padding: EdgeInsets.zero,
      children: [
        ColoredBox(
          color: palette.surface,
          child: Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
            child: Column(
              children: [
                _SectionHeading(
                  title: 'Recent',
                  action: history.isEmpty ? null : 'Clear',
                  onAction: history.isEmpty ? null : onClearHistory,
                ),
                if (history.isEmpty)
                  _EmptyHistory(color: palette.secondaryText)
                else
                  for (var index = 0; index < history.length; index++)
                    _HistoryItem(
                      word: history[index],
                      showDivider: index != history.length - 1,
                      onTap: () => onHistorySelected(history[index]),
                    ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 1),
        if (topics.isNotEmpty)
          ColoredBox(
            color: palette.surface,
            child: Padding(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _SectionHeading(
                    title: 'Browse by topic',
                    action: 'See all',
                    onAction: () => context.push(AppRoutes.topics),
                  ),
                  const SizedBox(height: 12),
                  LayoutBuilder(
                    builder: (context, constraints) {
                      final columns = constraints.maxWidth < 320 ? 1 : 2;
                      return GridView.builder(
                        shrinkWrap: true,
                        physics: const NeverScrollableScrollPhysics(),
                        itemCount: topics.length,
                        gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                          crossAxisCount: columns,
                          crossAxisSpacing: 8,
                          mainAxisSpacing: 8,
                          mainAxisExtent: 64,
                        ),
                        itemBuilder: (context, index) {
                          final topic = topics[index];
                          return _TopicCard(
                            topic: topic,
                            onTap: () => onTopicSelected(topic.topicId),
                          );
                        },
                      );
                    },
                  ),
                ],
              ),
            ),
          ),
      ],
    );
  }
}

class _SectionHeading extends StatelessWidget {
  const _SectionHeading({required this.title, this.action, this.onAction});

  final String title;
  final String? action;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    final palette = _SearchPalette.of(context);
    return SizedBox(
      height: 21,
      child: Row(
        children: [
          Text(
            title,
            style: AppTextStyles.label.copyWith(
              color: palette.text,
              fontSize: 14,
              height: 21 / 14,
              fontWeight: FontWeight.w600,
            ),
          ),
          const Spacer(),
          if (action != null)
            TextButton(
              key: Key(
                action == 'Clear'
                    ? 'clear-search-history'
                    : 'browse-all-topics',
              ),
              onPressed: onAction,
              style: TextButton.styleFrom(
                padding: EdgeInsets.zero,
                minimumSize: const Size(36, 21),
                tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                foregroundColor: palette.secondaryText,
              ),
              child: Text(
                action!,
                style: AppTextStyles.caption.copyWith(
                  fontSize: 13,
                  height: 19.5 / 13,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _EmptyHistory extends StatelessWidget {
  const _EmptyHistory({required this.color});

  final Color color;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 54,
      child: Align(
        alignment: Alignment.centerLeft,
        child: Text(
          'Your recent searches will appear here.',
          style: AppTextStyles.caption.copyWith(color: color, fontSize: 13),
        ),
      ),
    );
  }
}

class _HistoryItem extends StatelessWidget {
  const _HistoryItem({
    required this.word,
    required this.showDivider,
    required this.onTap,
  });

  final String word;
  final bool showDivider;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final palette = _SearchPalette.of(context);
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        child: Container(
          height: 46,
          decoration: BoxDecoration(
            border: showDivider
                ? Border(bottom: BorderSide(color: palette.border, width: 1))
                : null,
          ),
          child: Row(
            children: [
              Icon(Icons.history, size: 14, color: palette.secondaryText),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  word,
                  style: AppTextStyles.label.copyWith(
                    color: palette.text,
                    fontSize: 14,
                    height: 21 / 14,
                  ),
                ),
              ),
              Icon(Icons.chevron_right, size: 16, color: palette.secondaryText),
            ],
          ),
        ),
      ),
    );
  }
}

class _TopicCard extends StatelessWidget {
  const _TopicCard({required this.topic, required this.onTap});

  final TopicSummary topic;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final palette = _SearchPalette.of(context);
    return Material(
      color: palette.input,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: palette.border, width: 1.25),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        key: Key('browse-topic-${topic.topicId}'),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 13),
          child: Row(
            children: [
              SizedBox(
                width: 25,
                child: Text(
                  _topicEmoji(topic),
                  textAlign: TextAlign.center,
                  style: const TextStyle(fontSize: 18, height: 27 / 18),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      topic.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: AppTextStyles.label.copyWith(
                        color: palette.text,
                        fontSize: 13,
                        height: 19.5 / 13,
                      ),
                    ),
                    Text(
                      '${topic.wordCount} words',
                      maxLines: 1,
                      style: AppTextStyles.caption.copyWith(
                        color: palette.secondaryText,
                        fontSize: 11,
                        height: 16.5 / 11,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  String _topicEmoji(TopicSummary topic) {
    final value = '${topic.icon ?? ''} ${topic.name}'.toLowerCase();
    if (RegExp(r'[^\x00-\x7F]').hasMatch(topic.icon ?? '')) {
      return topic.icon!;
    }
    if (value.contains('academic') || value.contains('education')) return '🎓';
    if (value.contains('business') || value.contains('work')) return '💼';
    if (value.contains('technology') || value.contains('computer')) return '💻';
    if (value.contains('daily') || value.contains('life')) return '🌟';
    if (value.contains('travel') || value.contains('plane')) return '✈️';
    if (value.contains('ielts') || value.contains('exam')) return '📝';
    return '📚';
  }
}

class _EmptyResults extends StatelessWidget {
  const _EmptyResults({required this.isOffline});

  final bool isOffline;

  @override
  Widget build(BuildContext context) {
    final palette = _SearchPalette.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              isOffline ? Icons.cloud_off_outlined : Icons.search_off,
              size: 38,
              color: palette.secondaryText,
            ),
            const SizedBox(height: 12),
            Text(
              isOffline
                  ? 'No matching cached words'
                  : 'No matching words found',
              textAlign: TextAlign.center,
              style: AppTextStyles.button.copyWith(color: palette.text),
            ),
            const SizedBox(height: 4),
            Text(
              isOffline
                  ? 'Reconnect to search the full dictionary.'
                  : 'Try another spelling or adjust the filters.',
              textAlign: TextAlign.center,
              style: AppTextStyles.caption.copyWith(
                color: palette.secondaryText,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _LoadingSkeleton extends StatelessWidget {
  const _LoadingSkeleton();

  @override
  Widget build(BuildContext context) {
    final palette = _SearchPalette.of(context);
    return Shimmer.fromColors(
      baseColor: palette.input,
      highlightColor: palette.border.withValues(alpha: 0.55),
      child: ListView.separated(
        padding: const EdgeInsets.fromLTRB(20, 12, 20, 24),
        itemCount: 5,
        separatorBuilder: (_, _) => const SizedBox(height: 8),
        itemBuilder: (_, index) => Container(
          key: index == 0 ? const Key('search-loading-skeleton') : null,
          height: 102,
          decoration: BoxDecoration(
            color: palette.surface,
            borderRadius: BorderRadius.circular(14),
          ),
        ),
      ),
    );
  }
}

class _SearchPalette {
  const _SearchPalette({
    required this.background,
    required this.surface,
    required this.input,
    required this.text,
    required this.secondaryText,
    required this.border,
  });

  final Color background;
  final Color surface;
  final Color input;
  final Color text;
  final Color secondaryText;
  final Color border;

  factory _SearchPalette.of(BuildContext context) {
    final dark = Theme.of(context).brightness == Brightness.dark;
    return _SearchPalette(
      background: dark ? AppColors.background : const Color(0xFFF7F7F8),
      surface: dark ? AppColors.surface : Colors.white,
      input: dark ? const Color(0xFF292929) : const Color(0xFFF7F7F8),
      text: dark ? AppColors.onSurface : const Color(0xFF111111),
      secondaryText: dark ? const Color(0xFFAAA6B3) : const Color(0xFF9E9E9E),
      border: dark ? const Color(0xFF3A3A3A) : const Color(0xFFEFEFEF),
    );
  }
}
