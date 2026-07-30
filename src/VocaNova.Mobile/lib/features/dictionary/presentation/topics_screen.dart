import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/topic_display_name.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class TopicsScreen extends ConsumerStatefulWidget {
  const TopicsScreen({super.key});

  @override
  ConsumerState<TopicsScreen> createState() => _TopicsScreenState();
}

class _TopicsScreenState extends ConsumerState<TopicsScreen> {
  static const _categories = [
    'All',
    'Education',
    'Work',
    'Travel',
    'Daily life',
  ];
  late Future<List<TopicSummary>> _systemTopics;
  Future<List<TopicSummary>>? _personalTopics;
  bool _showPersonal = false;
  String _query = '';
  String _category = 'All';

  @override
  void initState() {
    super.initState();
    _systemTopics = ref.read(wordSearchRepositoryProvider).getTopics();
  }

  Future<List<TopicSummary>> get _currentTopics {
    if (!_showPersonal) return _systemTopics;
    return _personalTopics ??= ref
        .read(wordSearchRepositoryProvider)
        .getPersonalTopics()
        .then<List<TopicSummary>>((topics) => topics);
  }

  void _selectMode(bool personal) {
    if (_showPersonal == personal) return;
    setState(() => _showPersonal = personal);
  }

  void _reload() {
    if (_showPersonal) {
      _personalTopics = ref
          .read(wordSearchRepositoryProvider)
          .getPersonalTopics()
          .then<List<TopicSummary>>((topics) => topics);
    } else {
      _systemTopics = ref.read(wordSearchRepositoryProvider).getTopics();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(AppLocalizations.of(context)!.dictTopicsTitle),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          setState(_reload);
          await _currentTopics;
        },
        child: CustomScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
              sliver: SliverToBoxAdapter(
                child: _TopicModeSwitch(
                  showPersonal: _showPersonal,
                  onChanged: _selectMode,
                ),
              ),
            ),
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
              sliver: SliverToBoxAdapter(
                child: _ModeNote(personal: _showPersonal),
              ),
            ),
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 10),
              sliver: SliverToBoxAdapter(
                child: TextField(
                  key: const Key('topics-search'),
                  onChanged: (value) => setState(() => _query = value.trim()),
                  decoration: InputDecoration(
                    hintText: AppLocalizations.of(context)!.dictSearchTopicsHint,
                    prefixIcon: const Icon(Icons.search),
                  ),
                ),
              ),
            ),
            SliverToBoxAdapter(
              child: SizedBox(
                height: 48,
                child: ListView.separated(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  scrollDirection: Axis.horizontal,
                  itemCount: _categories.length,
                  separatorBuilder: (_, _) => const SizedBox(width: 8),
                  itemBuilder: (_, index) {
                    final category = _categories[index];
                    return FilterChip(
                      key: Key('topic-category-${category.toLowerCase()}'),
                      label: Text(_categoryLabel(context, category)),
                      selected: _category == category,
                      onSelected: (_) => setState(() => _category = category),
                    );
                  },
                ),
              ),
            ),
            FutureBuilder<List<TopicSummary>>(
              future: _currentTopics,
              builder: (context, snapshot) {
                if (snapshot.connectionState != ConnectionState.done) {
                  return const SliverFillRemaining(
                    child: Center(child: CircularProgressIndicator()),
                  );
                }
                if (snapshot.hasError) {
                  return SliverFillRemaining(
                    child: _TopicsError(onRetry: () => setState(_reload)),
                  );
                }
                final topics = (snapshot.data ?? const <TopicSummary>[])
                    .where(_matches)
                    .toList(growable: false);
                if (topics.isEmpty) {
                  return SliverFillRemaining(
                    child: Center(
                      child: Text(
                        _showPersonal
                            ? AppLocalizations.of(
                                context,
                              )!.dictNoPersonalTopicsMatch
                            : AppLocalizations.of(context)!.dictNoSystemTopics,
                      ),
                    ),
                  );
                }
                return SliverPadding(
                  padding: const EdgeInsets.all(16),
                  sliver: SliverGrid.builder(
                    gridDelegate:
                        const SliverGridDelegateWithFixedCrossAxisCount(
                          crossAxisCount: 2,
                          crossAxisSpacing: 12,
                          mainAxisSpacing: 12,
                          childAspectRatio: 1.14,
                        ),
                    itemCount: topics.length,
                    itemBuilder: (_, index) {
                      final topic = topics[index];
                      return _TopicGridCard(
                        topic: topic,
                        personal: _showPersonal,
                        onTap: () => context.push(
                          AppRoutes.topicDetail(
                            '${topic.topicId}',
                            personal: _showPersonal,
                          ),
                          extra: topic,
                        ),
                      );
                    },
                  ),
                );
              },
            ),
          ],
        ),
      ),
    );
  }

  bool _matches(TopicSummary topic) {
    final source = '${topic.name} ${topic.nameVi ?? ''}'.toLowerCase();
    if (_query.isNotEmpty && !source.contains(_query.toLowerCase())) {
      return false;
    }
    if (_category == 'All') return true;
    final category = _category.toLowerCase();
    const aliases = {
      'education': ['education', 'school', 'study', 'giáo dục', 'học'],
      'work': ['work', 'business', 'job', 'office', 'công việc'],
      'travel': ['travel', 'transport', 'du lịch'],
      'daily life': [
        'daily',
        'home',
        'food',
        'family',
        'hằng ngày',
        'gia đình',
      ],
    };
    return aliases[category]!.any(source.contains);
  }

  /// [category] is one of the internal English identifiers in [_categories]
  /// (also used for widget keys and [_matches] alias lookups); this only
  /// maps it to the localized label shown on the filter chip.
  String _categoryLabel(BuildContext context, String category) {
    final l10n = AppLocalizations.of(context)!;
    switch (category) {
      case 'Education':
        return l10n.dictCategoryEducation;
      case 'Work':
        return l10n.dictCategoryWork;
      case 'Travel':
        return l10n.dictCategoryTravel;
      case 'Daily life':
        return l10n.dictCategoryDailyLife;
      default:
        return l10n.dictCategoryAll;
    }
  }
}

class _TopicModeSwitch extends StatelessWidget {
  const _TopicModeSwitch({required this.showPersonal, required this.onChanged});

  final bool showPersonal;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 48,
      padding: const EdgeInsets.all(4),
      decoration: BoxDecoration(
        color: const Color(0xFFF1EEF6),
        borderRadius: BorderRadius.circular(15),
      ),
      child: Row(
        children: [
          Expanded(
            child: _ModeButton(
              key: const Key('topics-mode-system'),
              selected: !showPersonal,
              icon: Icons.public,
              label: AppLocalizations.of(context)!.dictSystemLibraryLabel,
              onTap: () => onChanged(false),
            ),
          ),
          Expanded(
            child: _ModeButton(
              key: const Key('topics-mode-personal'),
              selected: showPersonal,
              icon: Icons.auto_stories,
              label: AppLocalizations.of(context)!.dictMyTopicsLabel,
              onTap: () => onChanged(true),
            ),
          ),
        ],
      ),
    );
  }
}

class _ModeButton extends StatelessWidget {
  const _ModeButton({
    required this.selected,
    required this.icon,
    required this.label,
    required this.onTap,
    super.key,
  });

  final bool selected;
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: selected ? Colors.white : Colors.transparent,
      borderRadius: BorderRadius.circular(11),
      elevation: selected ? 1 : 0,
      child: InkWell(
        borderRadius: BorderRadius.circular(11),
        onTap: onTap,
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              icon,
              size: 17,
              color: selected ? AppColors.primary : const Color(0xFF77717D),
            ),
            const SizedBox(width: 7),
            Flexible(
              child: Text(
                label,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: selected
                      ? const Color(0xFF2F2440)
                      : const Color(0xFF77717D),
                  fontWeight: selected ? FontWeight.w700 : FontWeight.w600,
                  fontSize: 13,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ModeNote extends StatelessWidget {
  const _ModeNote({required this.personal});

  final bool personal;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(
          personal ? Icons.quiz_outlined : Icons.info_outline,
          size: 16,
          color: AppColors.primary,
        ),
        const SizedBox(width: 7),
        Expanded(
          child: Text(
            personal
                ? AppLocalizations.of(context)!.dictPersonalModeNote
                : AppLocalizations.of(context)!.dictSystemModeNote,
            style: Theme.of(
              context,
            ).textTheme.bodySmall?.copyWith(color: const Color(0xFF6F6876)),
          ),
        ),
      ],
    );
  }
}

class _TopicGridCard extends StatelessWidget {
  const _TopicGridCard({
    required this.topic,
    required this.personal,
    required this.onTap,
  });

  final TopicSummary topic;
  final bool personal;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        key: Key('topic-card-${topic.topicId}'),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Row(
                children: [
                  Text(
                    topic.icon?.trim().isNotEmpty == true ? topic.icon! : '📚',
                    style: const TextStyle(fontSize: 30),
                  ),
                  const Spacer(),
                  if (personal)
                    Icon(
                      topic.wordCount > 0
                          ? Icons.check_circle
                          : Icons.add_circle_outline,
                      size: 18,
                      color: topic.wordCount > 0
                          ? const Color(0xFF36A269)
                          : const Color(0xFFB6AFBD),
                    ),
                ],
              ),
              const SizedBox(height: 10),
              Text(
                topic.localizedName(context),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 4),
              Text(
                personal
                    ? AppLocalizations.of(
                        context,
                      )!.dictPersonalWordCount(topic.wordCount)
                    : AppLocalizations.of(
                        context,
                      )!.dictSystemWordCount(topic.wordCount),
                style: Theme.of(
                  context,
                ).textTheme.bodySmall?.copyWith(color: AppColors.primary),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _TopicsError extends StatelessWidget {
  const _TopicsError({required this.onRetry});
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => Center(
    child: Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(AppLocalizations.of(context)!.dictUnableToLoadTopics),
        TextButton(
          onPressed: onRetry,
          child: Text(AppLocalizations.of(context)!.dictTryAgain),
        ),
      ],
    ),
  );
}
