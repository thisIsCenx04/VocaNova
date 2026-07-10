import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';

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
  late Future<List<TopicSummary>> _topics;
  String _query = '';
  String _category = 'All';

  @override
  void initState() {
    super.initState();
    _reload();
  }

  void _reload() {
    _topics = ref.read(wordSearchRepositoryProvider).getTopics();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Topics')),
      body: RefreshIndicator(
        onRefresh: () async {
          setState(_reload);
          await _topics;
        },
        child: CustomScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 10),
              sliver: SliverToBoxAdapter(
                child: TextField(
                  key: const Key('topics-search'),
                  onChanged: (value) => setState(() => _query = value.trim()),
                  decoration: const InputDecoration(
                    hintText: 'Search topics...',
                    prefixIcon: Icon(Icons.search),
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
                      label: Text(category),
                      selected: _category == category,
                      onSelected: (_) => setState(() => _category = category),
                    );
                  },
                ),
              ),
            ),
            FutureBuilder<List<TopicSummary>>(
              future: _topics,
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
                  return const SliverFillRemaining(
                    child: Center(child: Text('No topics found.')),
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
                          childAspectRatio: 1.2,
                        ),
                    itemCount: topics.length,
                    itemBuilder: (_, index) {
                      final topic = topics[index];
                      return _TopicGridCard(
                        topic: topic,
                        onTap: () => context.push(
                          AppRoutes.topicDetail('${topic.topicId}'),
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
}

class _TopicGridCard extends StatelessWidget {
  const _TopicGridCard({required this.topic, required this.onTap});

  final TopicSummary topic;
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
              Text(
                topic.icon?.trim().isNotEmpty == true ? topic.icon! : '📚',
                style: const TextStyle(fontSize: 30),
              ),
              const SizedBox(height: 10),
              Text(
                topic.displayName,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 4),
              Text(
                '${topic.wordCount} words',
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
        const Text('Unable to load topics.'),
        TextButton(onPressed: onRetry, child: const Text('Try again')),
      ],
    ),
  );
}
